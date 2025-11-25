using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ModbusMqttPublisher.Server.Common
{
    public class DiagnosticTimeCounter
    {
        private class Holder : IDisposable
        {
            private readonly DiagnosticTimeCounter _parent;

            public Holder(DiagnosticTimeCounter parent)
            {
                _parent = parent;
            }

            public void Dispose()
            {
                _parent.Stop();
            }
        }

        private readonly Counter<double> _counter;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public DiagnosticTimeCounter(Counter<double> counter)
        {
            _counter = counter;
        }

        public void Start()
        {
            _stopwatch.Restart();
        }

        public void Stop()
        {
            _stopwatch.Stop();
            _counter.Add(_stopwatch.Elapsed.TotalMilliseconds);
            _stopwatch.Reset();
        }

        public IDisposable GetStartHolder()
        {
            Start();
            return new Holder(this);
        }
    }
}
