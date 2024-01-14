using NModbus;

namespace MudbusMqttPublisher.Server.Services
{
    public interface IConnectionPoolService
    {
        Task<IModbusMaster> GetConnection(string connectionId);
    }
}