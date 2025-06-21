using System.Diagnostics;

namespace ModbusMqttPublisher.Server.Services
{
	public class Profiler
	{
		Stopwatch globalStopwatch = new Stopwatch();
		Dictionary<string, Stopwatch> methods = new();

		public TimeSpan Elapsed => globalStopwatch.Elapsed;

		public void Start()
		{
			globalStopwatch.Restart();
			globalStopwatch.Start();
			methods = new();
		}

		public void Stop(out TimeSpan globalTime, out Dictionary<string, TimeSpan> methodsTimes)
		{
			globalStopwatch.Stop();
			globalTime = globalStopwatch.Elapsed;
			methodsTimes = new Dictionary<string, TimeSpan>(methods.Select(i => new KeyValuePair<string, TimeSpan>(i.Key, i.Value.Elapsed)));
		}

		public IDisposable StartMethod(string name)
		{
			if (!methods.TryGetValue(name, out var stopwatch))
			{
				stopwatch = new Stopwatch();
				methods[name] = stopwatch;
			}
			return new MethodHolder(stopwatch);
		}

		public void WrapMethod(string name, Action action)
		{
			using var holder = StartMethod(name);
			action();
		}

		public T WrapMethod<T>(string name, Func<T> action)
		{
			using var holder = StartMethod(name);
			return action();
		}

		public async Task WrapMethodAsync(string name, Func<Task> action)
		{
			using var holder = StartMethod(name);
			await action();
		}

		public async Task<T> WrapMethodAsync<T>(string name, Func<Task<T>> action)
		{
			using var holder = StartMethod(name);
			return await action();
		}

		class MethodHolder : IDisposable
		{
			Stopwatch stopwatch;
			
			public MethodHolder(Stopwatch stopwatch)
			{
				this.stopwatch = stopwatch;
				stopwatch.Start();
			}

			public void Dispose()
			{
				stopwatch.Stop();
			}
		}
	}
}
