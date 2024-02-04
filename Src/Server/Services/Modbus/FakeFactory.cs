using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
	public class FakeFactory : IModbusClientFactory
	{
		public IModbusClient Create(PortSettings settings, Profiler profiler)
		{
			return new FakeClient();
		}
	}
}
