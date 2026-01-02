using ModbusMqttPublisher.Server.Contracts;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public record ModbusRequest(
        byte SlaveAddress,
        ushort StartRegister,
        ushort RegisterCount,
        RegisterType RegisterType);
}
