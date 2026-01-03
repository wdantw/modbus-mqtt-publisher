using ModbusMqttPublisher.Server.Services.Modbus.Enums;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class WbEventConfig
    {
        public WbEventConfig(WBEventType eventType, ushort startRegister, WbEventPriority[] eventPriorities)
        {
            EventType = eventType;
            StartRegister = startRegister;
            EventPriorities = eventPriorities;
        }

        public WBEventType EventType { get; }

        public ushort StartRegister { get; }

        public WbEventPriority[] EventPriorities { get; }
    }
}
