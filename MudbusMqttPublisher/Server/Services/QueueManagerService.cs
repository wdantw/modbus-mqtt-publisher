using MudbusMqttPublisher.Server.Contracts.Settings;
using MudbusMqttPublisher.Server.Services.Configuration;

namespace MudbusMqttPublisher.Server.Services
{
    public class QueueManagerService : BackgroundService, IQueueManagerService
    {
        private readonly IConfigurationResolver settingsService;
        private readonly ILogger<QueueManagerService> logger;
        private readonly IQueueFactoryService queueFactoryService;
        private readonly IHost host;

        private readonly object synchObject = new object();
        private CancellationTokenSource currentCancellationTokenSource;
        private PortSettings[]? settings = null;

        public QueueManagerService(IConfigurationResolver settingsService, ILogger<QueueManagerService> logger, IQueueFactoryService queueFactoryService, IHost host)
        {
            this.settingsService = settingsService;
            this.logger = logger;
            this.queueFactoryService = queueFactoryService;
            this.host = host;

            currentCancellationTokenSource = new CancellationTokenSource();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Run(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Работа службы отменена");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Работа службы остановлена из за ошибки");
            }

            await host.StopAsync();
        }

        public string? GetTopicSerialName(string topicName)
        {
            if (settings == null)
                return null;

            foreach(var port in settings)
            {
                foreach(var  dev in port.Devices)
                {
                    foreach(var reg in dev.Registers)
                    {
                        if (reg.Name == topicName)
                            return port.SerialName;
                    }
                }
            }

            return null;
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            // TODO: цикл исключен,  так как в случае ошибки происходит непрерывный перезапуск
            //while (!stoppingToken.IsCancellationRequested)
            {
                CancellationTokenSource combinedCts;

                lock (synchObject)
                {
                    combinedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, currentCancellationTokenSource.Token);
                }

                settings = settingsService.ResolveConfigs();
                var tasks = settings
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
                logger.LogInformation($"Очередь {settings.SerialName} остановлена");
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Очередь {settings.SerialName} прекратила работу из за ошибки");
            }
        }

        public void ReloadSettings()
        {
            //lock (synchObject)
            //{
            //    var curr = currentCancellationTokenSource;
            //    currentCancellationTokenSource = new CancellationTokenSource();
            //    curr.Cancel();
            //}
        }
    }
}
