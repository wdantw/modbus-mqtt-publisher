using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services
{
    public interface IQueueFactoryService
    {
        public IQueueService CreateQueue(PortSettings settings);
    }
}
