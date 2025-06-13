using ModbusMqttPublisher.Server.Domain;
using ModbusMqttPublisher.Server.Services.Configuration;
using System.Collections.Frozen;

namespace ModbusMqttPublisher.Server.Services
{
    public class QueueManagerService : BackgroundService, IQueueManagerService
    {
        private readonly IConfigurationResolver settingsService;
        private readonly ILogger<QueueManagerService> logger;
        private readonly IQueueFactoryService queueFactoryService;
        private readonly IHost host;

        private readonly object synchObject = new object();
        private CancellationTokenSource currentCancellationTokenSource;
        private ReadPort[]? settings = null;
        private FrozenDictionary<string, string>? _portsByRegName = null;

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
            if (_portsByRegName == null)
                return null;

            if (!_portsByRegName.TryGetValue(topicName, out var serialName))
                return null;

            return serialName;
        }

        public async Task Run(CancellationToken stoppingToken)
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

            _portsByRegName = settings
                .SelectMany(p => p.Devices.SelectMany(d => d.Groups).SelectMany(g => g.Registers).Select(r => (Reg: r, Port: p)))
                .ToFrozenDictionary(x => x.Reg.Name, x => x.Port.SerialName);

            await Task.WhenAll(tasks);
        }

        private async Task RunSingle(ReadPort settings, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var queue = queueFactoryService.CreateQueue(settings);

                    await queue.Run(cancellationToken);
                    return;
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

                try
                {
                    await Task.Delay(settings.ErrorSleepTimeout, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
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
