using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services.Modbus
{
	public interface IModbusClientFactory
	{
		IModbusClient Create(PortSettings settings);
	}
}