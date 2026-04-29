namespace ModbusMqttPublisher.Server.Contracts.Configs
{
    public class PublisherMqttOptions
    {
        public const string SectionName = "MqttOptions";

        /// <summary>
        /// Базовая директория топиков для записи данных в устройства.
        /// </summary>
        public string BaseTopicPath { get; set; } = "Write";
    }
}
