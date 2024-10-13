using System;
using System.Threading;
using System.Threading.Tasks;

namespace ModbusMqttPublisher.Tests.Common
{
    public static class Utils
    {
        public static Task SpinUntil(Func<bool> condition, CancellationToken cancellationToken)
            => SpinUntil(condition, 100, cancellationToken);

        public static async Task SpinUntil(Func<bool> condition, int timeoutMilliseconds, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (condition())
                    return;

                await Task.Delay(timeoutMilliseconds, cancellationToken);
            }
        }

        public static CancellationToken CreateCancellationToken(int timeoutMilliseconds)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeoutMilliseconds);
            return cts.Token;
        }
    }
}
