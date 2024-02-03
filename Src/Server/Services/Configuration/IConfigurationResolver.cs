using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Services.Configuration
{
    public interface IConfigurationResolver
    {
        PortSettings[] ResolveConfigs();
    }
}