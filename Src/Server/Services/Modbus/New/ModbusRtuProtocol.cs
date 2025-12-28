using ModbusMqttPublisher.Server.Services.Modbus.New.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.New.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.New.Handlers;
using ModbusMqttPublisher.Server.Services.Modbus.New.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.New
{
    public class ModbusRtuProtocol
    {
        private const int MaxMessageSize = 256;

        private readonly IModbusChannel _channel;
        private readonly ModbusReadBuffer _readBuffer;
        private readonly ModbusWriteBuffer _writeBuffer;


        public ModbusRtuProtocol(IModbusChannel channel)
        {
            _channel = channel;

            var buffer = new byte[MaxMessageSize];
            _readBuffer = new ModbusReadBuffer(buffer, channel);
            _writeBuffer = new ModbusWriteBuffer(buffer);
        }

        public TResult PerformRequest<TResult>(IModbusRequestHandler<TResult> handler)
        {
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

        //private Span<byte> AnswerWbEvent(byte requestAddress, int requestRegisterCount)
        //{
        //    var (address, functionCode) = StartReadAnswer();

        //    if (functionCode != ModbusFunctionCode.WBExtendedFunction)
        //        throw new ModbusFormatException("Неверный код функции в ответе");

        //    var subCommand = (WirenBoardSubCommand)ReadChunk(1)[0];

        //    if (subCommand == WirenBoardSubCommand.EventsFinished)
        //    {
        //        ReadChunk(2);
        //        CheckCrc();
        //        return null;
        //    }
        //    else if (subCommand == WirenBoardSubCommand.EventsReceived)
        //    {
        //        var eventsHeader = ReadChunk(3);
        //        var acceptFlag = eventsHeader[0];
        //        var eventCount = eventsHeader[1];
        //        var dataByteCount = eventsHeader[2];

        //        ReadChunk(dataByteCount + 2);
        //        CheckCrc();
        //    }
        //    else
        //    {
        //        throw new ModbusFormatException("Неверный код субкоманды в ответе");
        //    }


        //    if (address != requestAddress)
        //        throw new ModbusFormatException("Неверный адрес устройства в ответе");


        //    var answerBytesCount = ReadChunk(1)[0];
        //    var extectedAnswerBytesCount = (requestRegisterCount + 7) / 8;
        //    if (extectedAnswerBytesCount != answerBytesCount)
        //        throw new ModbusFormatException("Размер данных в ответе не соответсвует ожидаемому");

        //    var dataBytes = ReadChunk(answerBytesCount + 2);
        //    CheckCrc();

        //    return dataBytes;
        //}

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
