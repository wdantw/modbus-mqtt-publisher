using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services.Configuration
{
    public interface IConfigurationResolver
    {
        ReadPort[] ResolveConfigs();
    }
}