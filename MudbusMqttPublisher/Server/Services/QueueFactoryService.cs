using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services
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
            switch (settings.PortType)
            {
                case Contracts.PortType.SerialModbus:
                    return ActivatorUtilities.CreateInstance<ModbusQueueService>(serviceProvider, settings);
                //case Contracts.PortType.OneWaire:
                default:
                    throw new Exception($"Неподдерживаемый тип порта {settings.PortType}");
            }
        }
    }
}
