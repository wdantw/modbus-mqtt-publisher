using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class WriteSingleCoilHandler : IModbusRequestHandler<NoResult>
    {
        private readonly byte _requestSlaveAddress;
        private readonly ushort _requestRegisterAddress;
        private readonly bool _requestValue;

        public WriteSingleCoilHandler(byte requestSlaveAddress, ushort requestRegisterAddress, bool requestValue)
        {
            _requestSlaveAddress = requestSlaveAddress;
            _requestRegisterAddress = requestRegisterAddress;
            _requestValue = requestValue;
        }

        public ModbusFunctionCode RequestFunctionCode => ModbusFunctionCode.WriteSingleCoil;

        public byte RequestSlaveAddress => _requestSlaveAddress;

        public bool SkeepStartWbArbitration => false;

        public void WriteRequest(IChannelDataWriter writer)
        {
            ByteOrderUtils.WriteBE(writer.Alloc(2), _requestRegisterAddress);
            ByteOrderUtils.WriteBE(writer.Alloc(2), _requestValue ? ModbusConstants.BitRegisterOn : ModbusConstants.BitRegisterOff);
        }

        public NoResult ReadAnswer(ModbusHeader header, IChannelDataReader reader)
        {
            var answRegAddr = ByteOrderUtils.ToUInt16BE(reader.Read(2));
            var answValue = ByteOrderUtils.ToUInt16BE(reader.Read(2));

            if (answRegAddr != _requestRegisterAddress)
                throw new ModbusFormatException("Неверный адрес регистра в ответе");

            if (answValue != (_requestValue ? ModbusConstants.BitRegisterOn : ModbusConstants.BitRegisterOff))
                throw new ModbusFormatException("Неверное значение регистра в ответе");

            return NoResult.Value;
        }

        public string GetRequestInformation()
        {
            return $"Device: {_requestSlaveAddress}. Command: Write single coil {_requestRegisterAddress}.";
        }
    }
}
