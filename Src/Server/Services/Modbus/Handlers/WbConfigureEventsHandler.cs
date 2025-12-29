using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class WbConfigureEventsHandler : IModbusRequestHandler<WbEventConfig[]>
    {
        private readonly byte _slaveAddress;
        private readonly WbEventConfig[] _configurations;

        public WbConfigureEventsHandler(byte slaveAddress, WbEventConfig[] configurations)
        {
            _slaveAddress = slaveAddress;
            _configurations = configurations;
        }

        public ModbusFunctionCode RequestFunctionCode => ModbusFunctionCode.WBExtendedFunction;

        public byte RequestSlaveAddress => _slaveAddress;

        public void WriteRequest(IChannelDataWriter writer)
        {
            var header = writer.Alloc(2);
            header[0] = (byte)WBSubCommand.EventsConfigure;
            header[1] = (byte)_configurations.Length;

            foreach (var config in _configurations)
            {
                var configDataHeader = writer.Alloc(4);
                configDataHeader[0] = (byte)config.EventType;
                ByteOrderUtils.WriteBE(configDataHeader.Slice(1, 2), config.StartRegister);
                configDataHeader[3] = (byte)config.EventPriorities.Length;
                var configData = writer.Alloc(config.EventPriorities.Length);
                for (int i = 0; i < config.EventPriorities.Length; i++)
                    configData[i] = (byte)config.EventPriorities[i];
            }
        }

        public WbEventConfig[] ReadAnswer(ModbusHeader header, IChannelDataReader reader)
        {
            var answerHeader = reader.Read(2);
            var subCommand = (WBSubCommand)answerHeader[0];
            var dataByteCount = answerHeader[1];

            if (subCommand != WBSubCommand.EventsConfigure)
                throw new ModbusFormatException("Неверный код субкоманды в ответе");

            var expextedDataBytes = 0;
            foreach (var config in _configurations)
                expextedDataBytes += (config.EventPriorities.Length + 7) / 8;

            if (expextedDataBytes != dataByteCount)
                throw new ModbusFormatException("Количество данных в ответе не соответсвует ожидаемому");

            var dataBytes = reader.Read(dataByteCount);
            var dataByteOffset = 0;
            var result = new WbEventConfig[_configurations.Length];

            for (int configIndex = 0; configIndex < result.Length; configIndex++)
            {
                var srcConfig = _configurations[configIndex];
                var resPriorities = new WbEventPriority[srcConfig.EventPriorities.Length];

                for (int registerIndex = 0; registerIndex < srcConfig.EventPriorities.Length; registerIndex++)
                {
                    var resFlag = (dataBytes[dataByteOffset + registerIndex / 8] & 1 << registerIndex % 8) != 0;

                    if (resFlag)
                    {
                        if (srcConfig.EventPriorities[registerIndex] == WbEventPriority.Disabled)
                            throw new ModbusFormatException("Устройсво увлючило событие, которое не запрашивалось");

                        resPriorities[registerIndex] = srcConfig.EventPriorities[registerIndex];
                    }
                    else
                    {
                        resPriorities[registerIndex] = WbEventPriority.Disabled;
                    }
                }

                dataByteOffset += (srcConfig.EventPriorities.Length + 7) / 8;
                result[configIndex] = new WbEventConfig(srcConfig.EventType, srcConfig.StartRegister, resPriorities);
            }

            return result;
        }
    }
}
