using ModbusMqttPublisher.Server.Services.Modbus.New.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.New.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.New.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.New.Handlers
{
    public class WriteSingleRegisterHandler : IModbusRequestHandler<NoResult>
    {
        private readonly byte _requestSlaveAddress;
        private readonly ushort _requestRegisterAddress;
        private readonly ushort _requestValue;

        public WriteSingleRegisterHandler(byte requestSlaveAddress, ushort requestRegisterAddress, ushort requestValue)
        {
            _requestSlaveAddress = requestSlaveAddress;
            _requestRegisterAddress = requestRegisterAddress;
            _requestValue = requestValue;
        }

        public ModbusFunctionCode RequestFunctionCode => ModbusFunctionCode.WriteSingleRegister;

        public byte RequestSlaveAddress => _requestSlaveAddress;

        public void WriteRequest(IChannelDataWriter writer)
        {
            ByteOrderUtils.WriteBE(writer.Alloc(2), _requestRegisterAddress);
            ByteOrderUtils.WriteBE(writer.Alloc(2), _requestValue);
        }

        public NoResult ReadAnswer(ModbusHeader header, IChannelDataReader reader)
        {
            var answRegAddr = ByteOrderUtils.ToUInt16BE(reader.Read(2));
            var answValue = ByteOrderUtils.ToUInt16BE(reader.Read(2));

            if (answRegAddr != _requestRegisterAddress)
                throw new ModbusFormatException("Неверный адрес регистра в ответе");

            if (answValue != _requestValue)
                throw new ModbusFormatException("Неверное значение регистра в ответе");

            return NoResult.Value;
        }
    }
}
