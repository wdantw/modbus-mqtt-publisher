namespace ModbusMqttPublisher.Server.Services
{
    public interface IQueueService
    {
        Task Run(CancellationToken cancellationToken);
    }
}
