namespace ModbusMqttPublisher.Server.Domain
{
    public static class IReadPriorityComparableExtension
    {
        /// <summary>
        /// Возвращает true если левый операнд (а) имеет больший приоритет по следующему чтению.
        /// </summary>
        /// <param name="a">Левый операнд</param>
        /// <param name="b">Правый операнд</param>
        /// <returns></returns>
        public static bool HasMorePriorityForRead(this IReadPriorityComparable a, IReadPriorityComparable b)
            => a.NextReadTime < b.NextReadTime;

        /// <summary>
        /// Возвращает true если подошло время чтения регистра
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="currTime">Текущее время</param>
        /// <returns></returns>
        public static bool NeedReadingNow(this IReadPriorityComparable item, DateTime currTime)
            => item.NextReadTime <= currTime;
    }
}
