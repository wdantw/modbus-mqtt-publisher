namespace ModbusMqttPublisher.Server.Contracts.Configs
{
    public class MqttOptions
    {
        public const string SectionName = "MqttOptions";

        public string? TcpAddress { get; set; }
        public TimeSpan? AutoReconnectDelay { get; set; }
        public TimeSpan? ConnectionCheckInterval { get; set; }
        public string BaseTopicPath { get; set; } = "ModbusWrite";
    }
}
