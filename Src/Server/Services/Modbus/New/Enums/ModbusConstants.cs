namespace ModbusMqttPublisher.Server.Services.Modbus.New.Enums
{
    public static class ModbusConstants
    {
        public const byte BroadcastAddress = 0xFD;

        public const ushort WordRegisterMaxReadPerRequest = 125;

        public const ushort WordRegisterMaxWritePerRequest = 123;

        public const ushort BitRegisterMaxReadPerRequest = 2000;
        
        public const ushort BitRegisterMaxWritePerRequest = 1968;

        public const ushort BitRegisterOn = 0xFF00;

        public const ushort BitRegisterOff = 0x0000;
    }
}
