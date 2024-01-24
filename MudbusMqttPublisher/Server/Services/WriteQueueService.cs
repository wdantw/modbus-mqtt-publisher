using MudbusMqttPublisher.Server.Common;
using MudbusMqttPublisher.Server.Contracts;
using System.Collections.Concurrent;

namespace MudbusMqttPublisher.Server.Services
{
    public class WriteQueueService : IWriteQueueService
    {
        private ConcurrentDictionary<string, AwaiteableQueue<WriteQuery>> queues = new();

        private AwaiteableQueue<WriteQuery> GetQueue(string serialName)
        {
            return queues.GetOrAdd(serialName, _ => new AwaiteableQueue<WriteQuery>());
        }

        public void AddWriteRequest(string serialName, string topicName, object value)
        {
            var queue = GetQueue(serialName);
            queue.Enqueue(new WriteQuery(topicName, value));
        }

        public Task WaitForItems(string serialName, CancellationToken cancellationToken) => GetQueue(serialName).WaitForItems(cancellationToken);

        public WriteQuery[] GetQueries(string serialName)
        {
            return GetQueue(serialName).TryGetAll();
        }

        public void AcceptDequeued(string serialName)
        {
            GetQueue(serialName).AcceptDequeued();
        }
    }
}
