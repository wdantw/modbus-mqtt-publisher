using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services
{
    public class SettingsService : ISettingsService
    {
        public PortSettings[] GetSettings()
        {
            return Array.Empty<PortSettings>();
        }
    }
}
