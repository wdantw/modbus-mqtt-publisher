namespace ModbusMqttPublisher.Server.Services.Modbus.New.Enums
{
    public enum ModbusErrorCode : byte
    {
        IllegalFunction = 0x1,
        IllegalDataAddress = 0x2,
        IllegalDataValue = 0x3,
        SlaveDeviceFailure = 0x4,
        Acknowlege = 0x5,
        SlaveDeviceBusy = 0x6,
        MemoryParityError = 0x8,
        GatewayPathUnavailable = 0xA,
        GatewayTargetDeviceFailedToRespond = 0xB,
    }
}
