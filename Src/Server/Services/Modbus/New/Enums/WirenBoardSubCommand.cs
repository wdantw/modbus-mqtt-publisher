namespace ModbusMqttPublisher.Server.Services.Modbus.New.Enums
{
    public enum WirenBoardSubCommand : byte
    {
        EventsRequest = 0x10,
        EventsReceived = 0x11,
        EventsFinished = 0x12,
    }
}
