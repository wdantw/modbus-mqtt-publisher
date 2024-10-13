using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModbusMqttPublisher.Tests.Common;
using System.Threading;
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

        public virtual Task OnHostStartedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder();
            hostBuilder.ConfigureServices(ConfigureServices);

            var startCancellationToken = Utils.CreateCancellationToken(10000);
            _host = await hostBuilder.StartAsync(startCancellationToken);
            await OnHostStartedAsync(startCancellationToken);
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
