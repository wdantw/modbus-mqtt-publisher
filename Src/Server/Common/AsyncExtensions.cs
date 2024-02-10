using System.Threading;
using System.Threading.Tasks;

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

		public static async Task<T> WhenCancelled<T>(this CancellationToken cancellationToken)
		{
			var tsc = new TaskCompletionSource<T>();
			using var regHolder = cancellationToken.Register(() => tsc.TrySetCanceled());
			return await tsc.Task;
		}

		public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
		{
			var tsc = new TaskCompletionSource();
			using var regHolder = cancellationToken.Register(() => tsc.TrySetCanceled());

			var winTask = await Task.WhenAny(task, tsc.Task);
			await winTask;
		}

		public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
		{
			var tsc = new TaskCompletionSource<T>();
			using var regHolder = cancellationToken.Register(() => tsc.TrySetCanceled());

			var winTask = await Task.WhenAny(task, tsc.Task);
			return await winTask;
		}

		public static async Task WithTimeoutNotThrow(this Task task, TimeSpan timeout)
		{
			var winTask = await Task.WhenAny(task, Task.Delay(timeout));
			await winTask;
		}
	}
}
