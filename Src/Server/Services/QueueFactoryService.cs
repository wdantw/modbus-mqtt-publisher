using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Services
{
    public class QueueFactoryService : IQueueFactoryService
    {
        private readonly IServiceProvider serviceProvider;

        public QueueFactoryService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IQueueService CreateQueue(PortSettings settings)
        {
            return ActivatorUtilities.CreateInstance<ModbusQueueService>(serviceProvider, settings);
        }
    }
}
