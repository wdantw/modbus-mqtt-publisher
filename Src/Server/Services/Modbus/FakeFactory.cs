using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public class FakeFactory : IModbusClientFactory
	{
		public IModbusClient Create(ReadPort settings, Profiler profiler)
		{
			return new FakeClient();
		}
	}
}
