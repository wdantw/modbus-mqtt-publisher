namespace ModbusMqttPublisher.Server.Common
{
    public static class AsyncExtensions
	{
		public static async Task WhenCancelled(this CancellationToken cancellationToken)
		{
			var tsc = new TaskCompletionSource();
			using var regHolder = cancellationToken.Register(() => tsc.TrySetCanceled());
			await tsc.Task;
		}

		public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
		{
			var tsc = new TaskCompletionSource();
			using var regHolder = cancellationToken.Register(() => tsc.TrySetCanceled());

			var winTask = await Task.WhenAny(task, tsc.Task);
			await winTask;
		}

		public static async Task<Task> WhenAnyCancellable(Func<CancellationToken, Task> func1, Func<CancellationToken, Task> func2, CancellationToken cancellationToken)
		{
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var task1 = func1(linkedCts.Token);
            var task2 = func2(linkedCts.Token);

            var winTask = await Task.WhenAny(task1, task2);

            linkedCts.Cancel();

			return winTask;
        }
	}
}
