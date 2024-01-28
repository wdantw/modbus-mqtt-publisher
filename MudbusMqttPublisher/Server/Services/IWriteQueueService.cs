using MudbusMqttPublisher.Server.Contracts;

namespace MudbusMqttPublisher.Server.Services
{
    public interface IWriteQueueService
    {
        void AcceptDequeued(string serialName);
        void AddWriteRequest(string serialName, string topicName, ArraySegment<byte> value);
        WriteQuery[] GetQueries(string serialName);
        Task WaitForItems(string serialName, CancellationToken cancellationToken);
    }
}