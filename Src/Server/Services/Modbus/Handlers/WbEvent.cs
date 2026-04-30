using ModbusMqttPublisher.Server.Services.Modbus.Enums;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class WbEvent
    {
        public WbEvent(WBEventType eventType, ushort eventId, byte[]? eventData)
        {
            EventType = eventType;
            EventId = eventId;
            EventData = eventData;
        }

        public WBEventType EventType { get; }

        public ushort EventId { get; }

        public byte[]? EventData { get; }
    }
}
