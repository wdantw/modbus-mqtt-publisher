using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public class FakeFactory : IModbusClientFactory
	{
		public IModbusClient Create(ReadPort settings)
		{
			return new FakeClient();
		}
	}
}
