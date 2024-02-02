using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services.Modbus
{
	public class ModbusClientFactory : IModbusClientFactory
	{
		IServiceProvider serviceProvider;

		public ModbusClientFactory(IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
		}

		public IModbusClient Create(PortSettings settings)
		{
			return ActivatorUtilities.CreateInstance<ModbusClient>(serviceProvider, settings);
		}
	}
}
