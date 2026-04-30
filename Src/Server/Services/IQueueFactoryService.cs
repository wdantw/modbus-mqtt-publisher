using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services
{
    public interface IQueueFactoryService
    {
        public IQueueService CreateQueue(ReadPort settings);
    }
}
