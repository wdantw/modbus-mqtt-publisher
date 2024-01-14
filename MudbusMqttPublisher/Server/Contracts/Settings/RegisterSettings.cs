namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class RegisterSettings
    {
        // имя топика mqtt
        public string Name { get; set; } = string.Empty;
        
        // номер регистра
        public int Number { get; set; }
        
        // тип регистра
        public RegisterType RegType { get; set; }

        public TimeSpan ReadPeriod { get; set; }
    }
}
