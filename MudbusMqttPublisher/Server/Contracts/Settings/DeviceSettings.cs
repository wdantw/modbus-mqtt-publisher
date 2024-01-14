namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class DeviceSettings
    {
        public string DeviceName { get; set; } = string.Empty;

        public RegisterSettings[] Registers { get; set; } = Array.Empty<RegisterSettings>();
    }
}
