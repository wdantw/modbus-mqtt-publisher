using System.Diagnostics;

namespace ModbusMqttPublisher.Server.Common
{
    public static class MonotonicTime
    {
        private static readonly long _startTicks = Stopwatch.GetTimestamp();

        public static TimeSpan TimeSinceStart
        {
            get
            {
                var ticks = Stopwatch.GetTimestamp() - _startTicks;
                var totalSeconds = ticks / Stopwatch.Frequency;
                var tickRest = ticks % Stopwatch.Frequency;
                double seconds = totalSeconds + ((double)tickRest / Stopwatch.Frequency);
                return TimeSpan.FromSeconds(seconds);
            }
        }
    }
}
