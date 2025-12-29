namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class WbEvents
    {
        public static readonly WbEvents Finished = new WbEvents(0, 0, 0, null);

        public WbEvents(byte eventCount, byte acceptFlag, byte slaveAddress, WbEvent[]? events)
        {
            EventCount = eventCount;
            AcceptFlag = acceptFlag;
            SlaveAddress = slaveAddress;
            Events = events;
        }

        public byte EventCount { get; }

        public byte AcceptFlag { get; }

        public byte SlaveAddress { get; }

        public WbEvent[]? Events { get; }
    }
}
