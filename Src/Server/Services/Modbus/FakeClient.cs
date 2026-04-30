using ModbusMqttPublisher.Server.Services.Modbus.Handlers;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public class FakeClient : IModbusClient
	{
		public void Dispose()
		{
		}

		public Task<bool[]> ReadBitRegistersAsync(ModbusRequest request, CancellationToken cancellationToken)
		{
			return Task.FromResult(new bool[request.RegisterCount]);
		}

		public Task<ushort[]> ReadShortRegistersAsync(ModbusRequest request, CancellationToken cancellationToken)
		{
			return Task.FromResult(new ushort[request.RegisterCount]);
		}

        public Task<WbEvents?> WbRequestEventsAsync(byte minSlaveAddress, byte acceptEventsSlaveAddress, byte acceptEventsFlag, CancellationToken cancellationToken)
        {
			return Task.FromResult<WbEvents?>(null);
        }

        public Task<WbEventConfig[]> WbConfigureEvents(byte slaveAddress, WbEventConfig[] configurations, CancellationToken cancellationToken)
        {
			return Task.FromResult(configurations);
        }

        public Task WriteBitRegistersAsync(ModbusRequest request, bool[] data, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public Task WriteShortRegistersAsync(ModbusRequest request, ushort[] data, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
