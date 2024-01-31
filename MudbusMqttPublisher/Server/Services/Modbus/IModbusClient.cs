namespace MudbusMqttPublisher.Server.Services.Modbus
{
	public interface IModbusClient : IDisposable
	{
		bool IsPortOpen { get; }
		Task CheckConnection(TimeSpan reconnectTimeout, CancellationToken cancellationToken);
		Task<bool[]> ReadBitRegistersAsync(ModbusRequest request);
		Task<ushort[]> ReadShortRegistersAsync(ModbusRequest request);
		Task WriteBitRegistersAsync(ModbusRequest request, bool[] data);
		Task WriteShortRegistersAsync(ModbusRequest request, ushort[] data);
	}
}