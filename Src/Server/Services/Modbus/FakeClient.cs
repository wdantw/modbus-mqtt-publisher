namespace MudbusMqttPublisher.Server.Services.Modbus
{
	public class FakeClient : IModbusClient
	{
		public bool IsPortOpen => throw new NotImplementedException();

		public Task CheckConnection(TimeSpan reconnectTimeout, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public void Dispose()
		{
		}

		public Task<bool[]> ReadBitRegistersAsync(ModbusRequest request)
		{
			return Task.FromResult(new bool[request.RegisterCount]);
		}

		public Task<ushort[]> ReadShortRegistersAsync(ModbusRequest request)
		{
			return Task.FromResult(new ushort[request.RegisterCount]);
		}

		public Task WriteBitRegistersAsync(ModbusRequest request, bool[] data)
		{
			return Task.CompletedTask;
		}

		public Task WriteShortRegistersAsync(ModbusRequest request, ushort[] data)
		{
			return Task.CompletedTask;
		}
	}
}
