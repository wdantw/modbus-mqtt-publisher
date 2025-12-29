using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.Handlers;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public class ModbusRtuProtocol
    {
        private readonly IModbusChannel _channel;
        private readonly ModbusReadBuffer _readBuffer;
        private readonly ModbusWriteBuffer _writeBuffer;

        public int RegtryCount { get; set; } = ModbusConstants.DefaultRetryCount;

        public ModbusRtuProtocol(IModbusChannel channel)
        {
            _channel = channel;

            var buffer = new byte[ModbusConstants.MaxModbusPacketSize];
            _readBuffer = new ModbusReadBuffer(buffer, channel);
            _writeBuffer = new ModbusWriteBuffer(buffer);
        }

        public TResult PerformRequest<TResult>(IModbusRequestHandler<TResult> handler)
        {
            // todo: реализовать ретраи
            return PerformRequestCore(handler);
        }


        private TResult PerformRequestCore<TResult>(IModbusRequestHandler<TResult> handler)
        {
            _channel.DiscardInBuffer();

            // ==============================================
            // отправка запроса
            // ==============================================

            _writeBuffer.Reset();

            // address + functionCode
            var outHeader = _writeBuffer.Alloc(2);
            outHeader[0] = handler.RequestSlaveAddress;
            outHeader[1] = (byte)handler.RequestFunctionCode;

            // main body
            handler.WriteRequest(_writeBuffer);

            // crc
            var crc = Crc16.CalculateLE(_writeBuffer.Whole());
            ByteOrderUtils.WriteLE(_writeBuffer.Alloc(2), crc);

            _channel.Write(_writeBuffer.Buffer, 0, _writeBuffer.Position);

            // ==============================================
            // получение ответа
            // ==============================================

            _readBuffer.Reset();

            // address + functionCode
            var inHeader = _readBuffer.Read(2);
            var inAddress = inHeader[0];
            var inFunctionCode = (ModbusFunctionCode)inHeader[1];

            if ((inFunctionCode & ModbusFunctionCode.ErrorCodeMask) != 0)
            {
                // errorCode + crc
                var errorBody = _readBuffer.Read(3);
                CheckCrc();
                throw new ModbusException(inAddress, inFunctionCode & ~ModbusFunctionCode.ErrorCodeMask, (ModbusErrorCode)errorBody[0]);
            }

            if (handler.RequestSlaveAddress != ModbusConstants.BroadcastAddress && inAddress != handler.RequestSlaveAddress)
                throw new ModbusFormatException("Неверный адрес устройства в ответе");

            if (inFunctionCode != handler.RequestFunctionCode)
                throw new ModbusFormatException("Неверный код функции в ответе");

            // main body
            var result = handler.ReadAnswer(new ModbusHeader(inAddress, inFunctionCode), _readBuffer);

            // crc
            _readBuffer.Read(2);
            CheckCrc();

            return result;
        }

        private void CheckCrc()
        {
            var wholeMessage = _readBuffer.Whole();

            var calculatedCrc = Crc16.CalculateLE(wholeMessage.Slice(0, wholeMessage.Length - 2));
            var messageCrc = ByteOrderUtils.ToUInt16LE(wholeMessage.Slice(wholeMessage.Length - 2));

            if (calculatedCrc != messageCrc)
            {
                var messageStr = BitConverter.ToString(_readBuffer.Buffer, 0, _readBuffer.Position);
                throw new ModbusCrcException($"Получен ответ с некорректной CRC суммой: {messageStr}");
            }
        }
    }
}
