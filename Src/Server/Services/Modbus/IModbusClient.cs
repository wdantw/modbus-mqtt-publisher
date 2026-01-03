using ModbusMqttPublisher.Server.Services.Modbus.Handlers;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
	public interface IModbusClient : IDisposable
	{
		Task<bool[]> ReadBitRegistersAsync(ModbusRequest request, CancellationToken cancellationToken);
		Task<ushort[]> ReadShortRegistersAsync(ModbusRequest request, CancellationToken cancellationToken);
		Task WriteBitRegistersAsync(ModbusRequest request, bool[] data, CancellationToken cancellationToken);
		Task WriteShortRegistersAsync(ModbusRequest request, ushort[] data, CancellationToken cancellationToken);
		Task<WbEvents?> WbRequestEventsAsync(byte minSlaveAddress, byte acceptEventsSlaveAddress, byte acceptEventsFlag, CancellationToken cancellationToken);
		Task<WbEventConfig[]> WbConfigureEvents(byte slaveAddress, WbEventConfig[] configurations, CancellationToken cancellationToken);
    }
}