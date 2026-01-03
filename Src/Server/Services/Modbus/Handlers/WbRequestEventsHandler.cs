using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class WbRequestEventsHandler : IModbusRequestHandler<WbEvents?>
    {
        private readonly byte _minSlaveAddress;
        private readonly byte _acceptEventsSlaveAddress;
        private readonly byte _acceptEventsFlag;

        public WbRequestEventsHandler(byte minSlaveAddress, byte acceptEventsSlaveAddress, byte acceptEventsFlag)
        {
            _minSlaveAddress = minSlaveAddress;
            _acceptEventsSlaveAddress = acceptEventsSlaveAddress;
            _acceptEventsFlag = acceptEventsFlag;
        }

        public WbRequestEventsHandler(byte minSlaveAddress)
        {
            _minSlaveAddress = minSlaveAddress;
            _acceptEventsSlaveAddress = 0;
            _acceptEventsFlag = 0;
        }

        public WbRequestEventsHandler()
        {
            _minSlaveAddress = 0;
            _acceptEventsSlaveAddress = 0;
            _acceptEventsFlag = 0;
        }

        public ModbusFunctionCode RequestFunctionCode => ModbusFunctionCode.WBExtendedFunction;

        public byte RequestSlaveAddress => ModbusConstants.BroadcastAddress;

        public bool SkeepStartWbArbitration => true;

        public void WriteRequest(IChannelDataWriter writer)
        {
            var requestData = writer.Alloc(5);
            requestData[0] = (byte)WBSubCommand.EventsRequest;
            requestData[1] = _minSlaveAddress;
            requestData[2] = ModbusConstants.MaxModbusPacketSize - 8; // максимальная длина поля данных с событиями в пакете, которую ожидает мастер
            requestData[3] = _acceptEventsSlaveAddress;
            requestData[4] = _acceptEventsFlag;
        }

        public WbEvents? ReadAnswer(ModbusHeader header, IChannelDataReader reader)
        {
            var answerSubComand = (WBSubCommand)reader.Read(1)[0];

            if (answerSubComand == WBSubCommand.EventsFinished)
            {
                if (header.AnswerAddress != ModbusConstants.BroadcastAddress)
                    throw new ModbusFormatException("Неверный адрес устройства в ответе");

                return null;
            }

            if (answerSubComand != WBSubCommand.EventsReceived)
                throw new ModbusFormatException("Неверный код субкоманды в ответе");

            if (header.AnswerAddress == ModbusConstants.BroadcastAddress)
                throw new ModbusFormatException("Неверный адрес устройства в ответе");

            var eventsMetadata = reader.Read(3);
            var acceptFlag = eventsMetadata[0];
            var eventCount = eventsMetadata[1];
            var dataByteCount = eventsMetadata[2];

            WbEvent[]? events = null;

            if (dataByteCount > 0)
            {
                var dataBytes = reader.Read(dataByteCount);

                events = new WbEvent[eventCount];
                int dataOffset = 0;
                int eventIndex = 0;
                while (eventIndex < eventCount && dataOffset < dataBytes.Length)
                {
                    var eventDataSize = dataBytes[dataOffset++];
                    var eventType = (WBEventType)dataBytes[dataOffset++];
                    var eventId = ByteOrderUtils.ToUInt16BE(dataBytes.Slice(dataOffset, 2));
                    dataOffset += 2;

                    byte[]? eventData = null;
                    if (eventDataSize > 0)
                    {
                        eventData = new byte[eventDataSize];
                        dataBytes.Slice(dataOffset, eventDataSize).CopyTo(eventData);
                        dataOffset += eventDataSize;
                    }

                    events[eventIndex++] = new WbEvent(eventType, eventId, eventData);
                }

                if (dataOffset != dataByteCount)
                    throw new ModbusFormatException("Неверный размер с данными о событиях");

                if (eventIndex < eventCount)
                {
                    // такое может быть если какое то соыбтие не влезло или из за глюка с System-Rebooted
                    var newEvents = new WbEvent[eventIndex];
                    Array.Copy(events, newEvents, eventIndex);
                    events = newEvents;
                }
            }

            return new WbEvents(eventCount, acceptFlag, header.AnswerAddress, events);
        }

        public string GetRequestInformation()
        {
            return $"Request wirenboard events. Min address: {_minSlaveAddress}. Accept address {_acceptEventsSlaveAddress} with flag {_acceptEventsFlag}";
        }
    }
}
