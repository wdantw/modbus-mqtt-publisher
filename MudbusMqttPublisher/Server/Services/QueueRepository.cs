namespace MudbusMqttPublisher.Server.Services
{

    public class QueueRepository : IQueueRepository
    {
        class Holder : IRepositoryRegisterHandle
        {
            public void Dispose()
            {
            }
        }
        public IRepositoryRegisterHandle RegisterQueue(IPortQueue queue, string portName)
        {
            return new Holder();
        }

    }
}
