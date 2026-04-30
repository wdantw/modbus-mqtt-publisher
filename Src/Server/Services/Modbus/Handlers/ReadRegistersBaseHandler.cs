using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public abstract class ReadRegistersBaseHandler<TResult> : IModbusRequestHandler<TResult>
    {
        private readonly byte _requestSlaveAddress;
        private readonly ushort _requestStartRegister;
        private readonly ushort _requestRegisterCount;

        protected ReadRegistersBaseHandler(byte requestSlaveAddress, ushort requestStartRegister, ushort requestRegisterCount)
        {
            _requestSlaveAddress = requestSlaveAddress;
            _requestStartRegister = requestStartRegister;
            _requestRegisterCount = requestRegisterCount;
        }

        public abstract ModbusFunctionCode RequestFunctionCode { get; }

        public byte RequestSlaveAddress => _requestSlaveAddress;

        public bool SkeepStartWbArbitration => false;

        public void WriteRequest(IChannelDataWriter writer)
        {
            ByteOrderUtils.WriteBE(writer.Alloc(2), _requestStartRegister);
            ByteOrderUtils.WriteBE(writer.Alloc(2), _requestRegisterCount);
        }

        protected abstract byte GetExpectedDataByteCount(ushort requestRegisterCount);

        protected abstract TResult GetResult(ReadOnlySpan<byte> dataBytes, ushort requestRegisterCount);

        public TResult ReadAnswer(ModbusHeader header, IChannelDataReader reader)
        {
            var answerBytesCount = reader.Read(1)[0];

            var extectedAnswerBytesCount = GetExpectedDataByteCount(_requestRegisterCount);
            if (answerBytesCount > extectedAnswerBytesCount)
                throw new ModbusFormatException("Размер данных в ответе больше ожидаемого");

            return GetResult(reader.Read(answerBytesCount), _requestRegisterCount);
        }

        public string GetRequestInformation()
        {
            return $"Device: {_requestSlaveAddress}. Command: read command {RequestFunctionCode.ToString()}. Start address: {_requestStartRegister}. Count: {_requestRegisterCount}";
        }
    }
}
