using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services
{
    public interface ISettingsService
    {
        PortSettings[] GetSettings();
    }
}