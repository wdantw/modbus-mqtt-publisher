using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public interface IModbusSerialPortFactory
    {
        IModbusSerialPort Create(ReadPort settings);
    }
}