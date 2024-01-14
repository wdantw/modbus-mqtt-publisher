
using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services
{
    public class QueueManagerService : IQueueManagerService
    {
        private readonly ISettingsService settingsService;
        private readonly ILogger<QueueManagerService> logger;
        private readonly IQueueFactoryService queueFactoryService;

        private readonly object synchObject = new object();
        private CancellationTokenSource currentCancellationTokenSource;

        public QueueManagerService(ISettingsService settingsService, ILogger<QueueManagerService> logger, IQueueFactoryService queueFactoryService)
        {
            this.settingsService = settingsService;
            this.logger = logger;
            this.queueFactoryService = queueFactoryService;

            currentCancellationTokenSource = new CancellationTokenSource();
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                CancellationTokenSource combinedCts;

                lock (synchObject)
                {
                    combinedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, currentCancellationTokenSource.Token);
                }

                var tasks = settingsService.GetSettings()
                    .Select(s => RunSingle(s, combinedCts.Token))
                    .Where(t => t != null)
                    .ToArray();

                await Task.WhenAll(tasks);
            }
        }

        private async Task RunSingle(PortSettings settings, CancellationToken cancellationToken)
        {
            //TODO: добавить Replay
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var queue = queueFactoryService.CreateQueue(settings);

                await queue.Run(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation($"Очередь {settings.PortName} остановлена");
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Очередь {settings.PortName} прекратила работу из за ошибки");
            }
        }

        public void ReloadSettings()
        {
            lock (synchObject)
            {
                var curr = currentCancellationTokenSource;
                currentCancellationTokenSource = new CancellationTokenSource();
                curr.Cancel();
            }
        }
    }
}
