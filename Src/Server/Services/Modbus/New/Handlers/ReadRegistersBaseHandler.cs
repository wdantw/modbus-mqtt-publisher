using ModbusMqttPublisher.Server.Services.Modbus.New.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.New.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.New.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.New.Handlers
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
            if (extectedAnswerBytesCount != answerBytesCount)
                throw new ModbusFormatException("Размер данных в ответе не соответсвует ожидаемому");

            return GetResult(reader.Read(answerBytesCount), _requestRegisterCount);
        }
    }
}
