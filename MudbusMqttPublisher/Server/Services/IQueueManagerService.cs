namespace MudbusMqttPublisher.Server.Services
{
    public interface IQueueManagerService
    {
        void ReloadSettings();
        Task Run(CancellationToken stoppingToken);
    }
}