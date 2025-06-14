namespace ModbusMqttPublisher.Server.Contracts.Configs
{
    public class MqttOptions
    {
        public const string SectionName = "MqttOptions";

        /// <summary>
        /// Адрес сервера Mqtt
        /// </summary>
        public string? TcpAddress { get; set; }
        
        /// <summary>
        /// Задержка между переконнектом
        /// </summary>
        public TimeSpan? AutoReconnectDelay { get; set; }
        
        /// <summary>
        /// Интервал проверки соединения
        /// </summary>
        public TimeSpan? ConnectionCheckInterval { get; set; }
        
        /// <summary>
        /// Базовая директория топиков для записи данных в устройства.
        /// </summary>
        public string BaseTopicPath { get; set; } = "ModbusWrite";
    }
}
