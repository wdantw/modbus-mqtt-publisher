namespace MudbusMqttPublisher.Server.Services
{
    public interface IQueueService : IPortQueue
    {
        Task Run(CancellationToken cancellationToken);
    }
}
