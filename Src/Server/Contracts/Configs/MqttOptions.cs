namespace MudbusMqttPublisher.Server.Contracts.Configs
{
    public class MqttOptions
    {
        public const string SectionName = "MqttOptions";

        public string? TcpAddress { get; set; }
        public string BaseTopicPath { get; set; } = "ModbusWrite";
    }
}
