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

        public ModbusRtuProtocol(IModbusChannel channel)
        {
            _channel = channel;

            //var buffer = new byte[ModbusConstants.MaxModbusPacketSize];
            _readBuffer = new ModbusReadBuffer(new byte[ModbusConstants.MaxModbusPacketSize], channel);
            _writeBuffer = new ModbusWriteBuffer(new byte[ModbusConstants.MaxModbusPacketSize]);
        }

        public TResult PerformRequest<TResult>(IModbusRequestHandler<TResult> handler)
        {
            _channel.DiscardInBuffer();
            _writeBuffer.Reset();
            _readBuffer.Reset();

            // ==============================================
            // отправка запроса
            // ==============================================

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

            byte inAddress;
            ModbusFunctionCode inFunctionCode;
            int skipedBytes = 0;
            if (handler.SkeepStartWbArbitration)
            {
                while (true)
                {
                    var b = _readBuffer.Read(1)[0];
                    if (b == 0xFF)
                    {
                        skipedBytes++;
                        continue;
                    }

                    inAddress = b;
                    break;
                }
                inFunctionCode = (ModbusFunctionCode)_readBuffer.Read(1)[0];
            }
            else
            {
                // address + functionCode
                var inHeader = _readBuffer.Read(2);
                inAddress = inHeader[0];
                inFunctionCode = (ModbusFunctionCode)inHeader[1];
            }

            if ((inFunctionCode & ModbusFunctionCode.ErrorCodeMask) != 0)
            {
                // errorCode + crc
                var errorBody = _readBuffer.Read(3);
                CheckCrc(skipedBytes);
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
            CheckCrc(skipedBytes);

            return result;
        }

        private void CheckCrc(int skipedBytes)
        {
            var wholeMessage = _readBuffer.Whole().Slice(skipedBytes);

            var calculatedCrc = Crc16.CalculateLE(wholeMessage.Slice(0, wholeMessage.Length - 2));
            var messageCrc = ByteOrderUtils.ToUInt16LE(wholeMessage.Slice(wholeMessage.Length - 2));

            if (calculatedCrc != messageCrc)
            {
                var messageStr = BitConverter.ToString(_readBuffer.Buffer, 0, _readBuffer.Position);
                throw new ModbusCrcException($"Получен ответ с некорректной CRC суммой: {messageStr}");
            }
        }

        public string GetLastRequestData()
        {
            var requestStr = BitConverter.ToString(_writeBuffer.Buffer, 0, _writeBuffer.Position);
            var answerStr = BitConverter.ToString(_readBuffer.Buffer, 0, _readBuffer.Position);
            return ">> " + requestStr + Environment.NewLine + "<< " + answerStr;
        }
    }
}
