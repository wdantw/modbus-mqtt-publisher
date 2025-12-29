namespace ModbusMqttPublisher.Server.Services.Modbus.Enums
{
    public enum ModbusFunctionCode : byte
    {
        // Bit access

        // Physical Discrete Inputs
        ReadDiscreteInputs = 0x02,

        // Internal Bits Or Physical coils
        ReadCoils = 0x01,
        WriteSingleCoil = 0x05,
        WriteMultipleCoils = 0x0F,

        // 16 bits access

        // Physical Input Registers
        ReadInputRegisters = 0x04,

        // Internal Registers Or Physical Output Registers
        ReadHoldingRegisters = 0x03,
        WriteSingleRegister = 0x06,
        WriteMultipleRegisters = 0x10,

        // Error
        ErrorCodeMask = 0x80,

        // Other
        WBExtendedFunction = 0x46,
    }
}
