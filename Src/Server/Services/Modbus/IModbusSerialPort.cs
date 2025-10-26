using NModbus.IO;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public interface IModbusSerialPort : IStreamResource
    {
        Task CheckConnection(TimeSpan reconnectTimeout, CancellationToken cancellationToken);
    }
}
