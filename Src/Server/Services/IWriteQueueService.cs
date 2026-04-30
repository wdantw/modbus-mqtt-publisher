using ModbusMqttPublisher.Server.Contracts;
using System.Buffers;

namespace ModbusMqttPublisher.Server.Services
{
    public interface IWriteQueueService
    {
        void AcceptDequeued(string serialName);

        void AddWriteRequest(string serialName, string topicName, ReadOnlySequence<byte> value);

        WriteQuery? GetQuery(string serialName);

        Task WaitForItems(string serialName, CancellationToken cancellationToken);
    }
}