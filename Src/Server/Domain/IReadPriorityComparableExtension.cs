namespace ModbusMqttPublisher.Server.Domain
{
    public static class IReadPriorityComparableExtension
    {
        public static bool HasMorePriorityForRead(this IReadPriorityComparable a, IReadPriorityComparable b)
            => a.NextReadTime < b.NextReadTime;

        public static bool NeedReading(this IReadPriorityComparable item)
            => item.NextReadTime < DateTime.MaxValue;

        public static bool NeedReadingNow(this IReadPriorityComparable item, DateTime currTime)
            => item.NextReadTime <= currTime;
    }
}
