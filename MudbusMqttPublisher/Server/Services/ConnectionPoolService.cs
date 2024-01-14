using NModbus;

namespace MudbusMqttPublisher.Server.Services
{
    public class ConnectionPoolService : IConnectionPoolService
    {
        public async Task<IModbusMaster> GetConnection(string connectionId)
        {
            await Task.Yield();
            return null;
        }
    }
}
