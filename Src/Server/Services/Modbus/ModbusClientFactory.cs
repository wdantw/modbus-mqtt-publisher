using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public class ModbusClientFactory : IModbusClientFactory
	{
		IServiceProvider serviceProvider;

		public ModbusClientFactory(IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
		}

		public IModbusClient Create(ReadPort settings)
		{
			return ActivatorUtilities.CreateInstance<ModbusClient>(serviceProvider, settings);
		}
	}
}
