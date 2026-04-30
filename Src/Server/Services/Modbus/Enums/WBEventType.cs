namespace ModbusMqttPublisher.Server.Services.Modbus.Enums
{
    public enum WBEventType : byte
    {
        Coil = 1,
        Discrete = 2,
        Holding = 3,
        Input = 4,
        System = 15,
    }
}
