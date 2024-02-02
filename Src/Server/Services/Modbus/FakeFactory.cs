using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services.Modbus
{
	public class FakeFactory : IModbusClientFactory
	{
		public IModbusClient Create(PortSettings settings)
		{
			return new FakeClient();
		}
	}
}
