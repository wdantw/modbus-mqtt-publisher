using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public interface IModbusClientFactory
	{
		IModbusClient Create(ReadPort settings);
	}
}