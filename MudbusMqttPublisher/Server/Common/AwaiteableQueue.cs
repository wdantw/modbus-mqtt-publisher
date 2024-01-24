using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace MudbusMqttPublisher.Server.Common
{
    public class AwaiteableQueue<TItem>
    {
        private ConcurrentQueue<TItem> queue = new();
        private TaskCompletionSource hasItemsTcs = new();
        private TItem[]? dequeued = null;

        public void Enqueue(TItem item)
        {
            queue.Enqueue(item);
            hasItemsTcs.TrySetResult();
        }

        public Task WaitForItems() => hasItemsTcs.Task;
        
        public Task WaitForItems(CancellationToken cancellationToken) => Task.WhenAny(WaitForItems(), Task.Delay(Timeout.Infinite, cancellationToken));

        public bool TryDequeue([MaybeNullWhen(false)] out TItem item)
        {
            var result = queue.TryDequeue(out item);

            hasItemsTcs = new TaskCompletionSource();

            if (!queue.IsEmpty)
                hasItemsTcs.TrySetResult();

            return result;
        }

        public TItem[] TryDequeueAll()
        {
            var items = new List<TItem>();
            while (queue.TryDequeue(out var item))
                items.Add(item);

            hasItemsTcs = new TaskCompletionSource();

            if (!queue.IsEmpty)
                hasItemsTcs.TrySetResult();

            return items.ToArray();
        }

        public bool TryGet([MaybeNullWhen(false)] out TItem item)
        {
            if (dequeued != null)
            {
                if (dequeued.Length != 1)
                    throw new Exception("Нельзя одновременно использовать и TryGet и TryGetAll");

                item = dequeued[0];
                return true;
            }

            var result = queue.TryDequeue(out item);

            if (result)
            {
                dequeued = new TItem[] { item! };
            }

            hasItemsTcs = new TaskCompletionSource();

            if (!queue.IsEmpty)
                hasItemsTcs.TrySetResult();

            return result;
        }

        public TItem[] TryGetAll()
        {
            if (dequeued != null)
            {
                return dequeued;
            }

            var items = new List<TItem>();
            while (queue.TryDequeue(out var item))
                items.Add(item);

            var itemArr = items.ToArray();
            dequeued = itemArr.Length > 0 ? itemArr : null;

            hasItemsTcs = new TaskCompletionSource();

            if (!queue.IsEmpty)
                hasItemsTcs.TrySetResult();

            return itemArr;
        }

        public void AcceptDequeued()
        {
            dequeued = null;
        }
    }
}
