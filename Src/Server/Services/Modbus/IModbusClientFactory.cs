using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
	public interface IModbusClientFactory
	{
		IModbusClient Create(PortSettings settings, Profiler profiler);
	}
}