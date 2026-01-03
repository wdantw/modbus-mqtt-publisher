namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public interface IModbusSerialPort : IModbusChannel, IDisposable
    {
        int InfiniteTimeout { get; }

        int ReadTimeout { get; set; }

        int WriteTimeout { get; set; }

        Task CheckConnection(TimeSpan reconnectTimeout, CancellationToken cancellationToken);
    }
}
