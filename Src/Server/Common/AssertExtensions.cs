using System.Runtime.CompilerServices;

namespace ModbusMqttPublisher.Server.Common
{
    public static class AssertExtensions
    {
        public static T AssertNotNull<T>(this T? instance, [CallerArgumentExpression(nameof(instance))] string varName = "value")
            where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(varName);

            return instance;
        }

        public static T AssertNotNull<T>(this T? instance, [CallerArgumentExpression(nameof(instance))] string varName = "value")
            where T : struct
        {
            if (!instance.HasValue)
                throw new ArgumentNullException(varName);

            return instance.Value;
        }

        public static string AssertNotEmpty(this string? instance, [CallerArgumentExpression(nameof(instance))] string varName = "value")
        {
            if (string.IsNullOrWhiteSpace(instance))
                throw new ArgumentNullException(varName);

            return instance;
        }
    }
}
