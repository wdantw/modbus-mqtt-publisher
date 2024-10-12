using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace ModbusMqttPublisher.Tests.Tests.Mqtt
{
    public static class Utils
    {
        public const string MosquittoServiceName = "mosquitto";

        public static async Task WaitForStatusAsync(this ServiceController serviceController, ServiceControllerStatus desiredStatus, CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(desiredStatus))
                throw new ArgumentException($"{nameof(desiredStatus)} has invalid value {desiredStatus}");

            await SpinUntil(() => {
                serviceController.Refresh();
                return serviceController.Status == desiredStatus;
            }, cancellationToken);
        }

        public static void CheckServiceStatus(this ServiceController serviceController, ServiceControllerStatus status)
        {
            var currentStatus = serviceController.Status;
            if (currentStatus != status)
                throw new ApplicationException($"Service \"{serviceController.ServiceName}\" is not in status {status} (current status: {currentStatus})");
        }

        public static async Task StartMosquitto(CancellationToken cancellationToken = default)
        {
            var serviceController = new ServiceController(MosquittoServiceName);

            if (serviceController.Status == ServiceControllerStatus.Stopped)
            {
                serviceController.Start();
                await serviceController.WaitForStatusAsync(ServiceControllerStatus.Running, cancellationToken);
            }

            serviceController.CheckServiceStatus(ServiceControllerStatus.Running);
        }

        public static async Task RestartMosquitto(CancellationToken cancellationToken = default)
        {
            var serviceController = new ServiceController(MosquittoServiceName);

            serviceController.CheckServiceStatus(ServiceControllerStatus.Running);

            serviceController.Stop();
            await serviceController.WaitForStatusAsync(ServiceControllerStatus.Stopped, cancellationToken);

            serviceController.Start();
            await serviceController.WaitForStatusAsync(ServiceControllerStatus.Running, cancellationToken);
        }

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
    }
}
