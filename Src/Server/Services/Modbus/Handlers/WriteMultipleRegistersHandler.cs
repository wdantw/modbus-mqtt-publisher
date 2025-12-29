using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class WriteMultipleRegistersHandler : IModbusRequestHandler<NoResult>
    {
        private readonly byte _requestSlaveAddress;
        private readonly ushort _requestStartRegister;
        private readonly ushort[] _requestValues;

        public WriteMultipleRegistersHandler(byte requestSlaveAddress, ushort requestStartRegister, ushort[] requestValues)
        {
            if (requestValues.Length > ModbusConstants.BitRegisterMaxWritePerRequest)
                throw new ArgumentOutOfRangeException(nameof(requestValues));

            _requestSlaveAddress = requestSlaveAddress;
            _requestStartRegister = requestStartRegister;
            _requestValues = requestValues;
        }

        public ModbusFunctionCode RequestFunctionCode => ModbusFunctionCode.WriteMultipleCoils;

        public byte RequestSlaveAddress => _requestSlaveAddress;

        public void WriteRequest(IChannelDataWriter writer)
        {
            var bytesCount = (byte)(_requestValues.Length * 2);

            ByteOrderUtils.WriteBE(writer.Alloc(2), _requestStartRegister);
            ByteOrderUtils.WriteBE(writer.Alloc(2), (ushort)_requestValues.Length);
            writer.Alloc(1)[0] = bytesCount;

            var dataBytes = writer.Alloc(bytesCount);

            for (int index = 0; index < _requestValues.Length; index++)
            {
                ByteOrderUtils.WriteBE(dataBytes.Slice(index * 2, 2), _requestValues[index]);
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
    }
}
