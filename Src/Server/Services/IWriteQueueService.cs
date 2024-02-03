using ModbusMqttPublisher.Server.Contracts;

namespace ModbusMqttPublisher.Server.Services
{
    public interface IWriteQueueService
    {
        void AcceptDequeued(string serialName);
        void AddWriteRequest(string serialName, string topicName, ArraySegment<byte> value);
        WriteQuery? GetQuery(string serialName);
        Task WaitForItems(string serialName, CancellationToken cancellationToken);
    }
}