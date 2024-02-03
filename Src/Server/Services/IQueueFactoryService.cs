using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Services
{
    public interface IQueueFactoryService
    {
        public IQueueService CreateQueue(PortSettings settings);
    }
}
