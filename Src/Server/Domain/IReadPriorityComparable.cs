namespace ModbusMqttPublisher.Server.Domain
{
    public interface IReadPriorityComparable
    {
        DateTime NextReadTime { get; }
    }
}
