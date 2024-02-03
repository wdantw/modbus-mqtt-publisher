namespace ModbusMqttPublisher.Server.Common
{
    public static class EnumerableExtension
    {
        public static IEnumerable<T> FirstAndFilterdByFirst<T>(this IEnumerable<T> source, Func<T, T, bool> filter)
        {
            bool first = true;
            T firstItem = default!;
            foreach (var item in source)
            {
                if (first)
                {
                    first = false;
                    firstItem = item;
                    yield return item;
                }
                else
                {
                    if (filter(firstItem, item))
                    {
                        yield return item;
                    }
                }
            }
        }

        public static IEnumerable<T> TakeOnlyFirstIf<T>(this IEnumerable<T> source, Func<T, bool> clause)
        {
            bool first = true;
            foreach (var item in source)
            {
                if (first)
                {
                    first = false;
                    yield return item;
                    if (clause(item))
                    {
                        yield break;
                    }
                }
                else
                {
                    yield return item;
                }
            }
        }


        public static IEnumerable<T> TakeWhileWithPrev<T>(this IEnumerable<T> source, Func<T, T, bool> predicate)
        {
            bool first = true;
            T prevItem = default!;
            foreach (var item in source)
            {
                if (first)
                {
                    first = false;
                    prevItem = item;
                    yield return item;
                }
                else
                {
                    if (!predicate(prevItem, item))
                    {
                        yield break;
                    }
                    prevItem = item;
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> WhereWithPrev<T>(this IEnumerable<T> source, Func<T, T, bool> filter)
        {
            bool first = true;
            T prevItem = default!;
            foreach (var item in source)
            {
                if (first)
                {
                    first = false;
                    prevItem = item;
                    yield return item;
                }
                else
                {
                    if (filter(prevItem, item))
                    {
                        yield return item;
                    }
                    prevItem = item;
                }
            }
        }
    }
}
