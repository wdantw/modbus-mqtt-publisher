namespace MudbusMqttPublisher.Server.Services
{
    public class MainWorker : BackgroundService
    {
        private readonly ILogger<MainWorker> logger;
        private readonly IHost host;
        private readonly IQueueManagerService queueManager;

        public MainWorker(ILogger<MainWorker> logger, IHost host, IQueueManagerService queueManager)
        {
            this.logger = logger;
            this.host = host;
            this.queueManager = queueManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await queueManager.Run(stoppingToken);
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
    }
}
