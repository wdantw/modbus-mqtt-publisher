namespace ModbusMqttPublisher.Server.Services.Modbus.Enums
{
    public enum WBSubCommand : byte
    {
        EventsRequest = 0x10,
        EventsReceived = 0x11,
        EventsFinished = 0x12,
        EventsConfigure = 0x18,
    }
}
