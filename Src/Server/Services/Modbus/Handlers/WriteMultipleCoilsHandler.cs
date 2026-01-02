using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class WriteMultipleCoilsHandler : IModbusRequestHandler<NoResult>
    {
        private readonly byte _requestSlaveAddress;
        private readonly ushort _requestStartRegister;
        private readonly bool[] _requestValues;

        public WriteMultipleCoilsHandler(byte requestSlaveAddress, ushort requestStartRegister, bool[] requestValues)
        {
            if (requestValues.Length > ModbusConstants.BitRegisterMaxWritePerRequest)
                throw new ArgumentOutOfRangeException(nameof(requestValues));

            _requestSlaveAddress = requestSlaveAddress;
            _requestStartRegister = requestStartRegister;
            _requestValues = requestValues;
        }

        public ModbusFunctionCode RequestFunctionCode => ModbusFunctionCode.WriteMultipleCoils;

        public byte RequestSlaveAddress => _requestSlaveAddress;

        public bool SkeepStartWbArbitration => false;

        public void WriteRequest(IChannelDataWriter writer)
        {
            var bytesCount = (byte)((_requestValues.Length + 7) / 8);

            ByteOrderUtils.WriteBE(writer.Alloc(2), _requestStartRegister);
            ByteOrderUtils.WriteBE(writer.Alloc(2), (ushort)_requestValues.Length);
            writer.Alloc(1)[0] = bytesCount;

            var dataBytes = writer.Alloc(bytesCount);

            for (int index = 0; index < _requestValues.Length; index++)
            {
                if (_requestValues[index])
                {
                    dataBytes[index / 8] |= (byte)(1 << index % 8);
                }
            }
        }

        public NoResult ReadAnswer(ModbusHeader header, IChannelDataReader reader)
        {
            var answRegAddr = ByteOrderUtils.ToUInt16BE(reader.Read(2));
            var answQuantity = ByteOrderUtils.ToUInt16BE(reader.Read(2));

            if (answRegAddr != _requestStartRegister)
                throw new ModbusFormatException("Неверный адрес регистра в ответе");

            if (answQuantity != _requestValues.Length)
                throw new ModbusFormatException("Неверное количество регистров в ответе");

            return NoResult.Value;
        }

        public string GetRequestInformation()
        {
            return $"Device: {_requestSlaveAddress}. Command: Write multiple coils. Start address: {_requestStartRegister}. Count: {_requestValues?.Length}";
        }
    }
}
