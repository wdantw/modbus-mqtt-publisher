using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services.Configuration
{
    public interface IConfigurationResolver
    {
        PortSettings[] ResolveConfigs();
    }
}