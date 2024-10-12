using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Xunit;

namespace ModbusMqttPublisher.Tests.Hosts
{
    public class BaseTestHost : IAsyncLifetime
    {
        private IHost _host = null!;

        public IHost Host => _host;

        public virtual void ConfigureServices(IServiceCollection services)
        {
        }

        public async Task InitializeAsync()
        {
            var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder();
            hostBuilder.ConfigureServices(ConfigureServices);
            
            _host = await hostBuilder.StartAsync();
        }

        public async Task DisposeAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
                _host = null!;
            }
        }
    }
}
