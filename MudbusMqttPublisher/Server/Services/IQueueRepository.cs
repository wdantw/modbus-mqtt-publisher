using MudbusMqttPublisher.Server.Contracts;

namespace MudbusMqttPublisher.Server.Services
{
    public interface IRepositoryRegisterHandle : IDisposable
    {

    }
    public interface IQueueRepository
    {
        IRepositoryRegisterHandle RegisterQueue(IPortQueue queue, string portName);
    }
}
