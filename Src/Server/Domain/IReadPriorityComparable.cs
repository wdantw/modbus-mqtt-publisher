namespace ModbusMqttPublisher.Server.Domain
{
    public interface IReadPriorityComparable<T>
    {
        bool HasMorePriorityForRead(T other);
    }
}
