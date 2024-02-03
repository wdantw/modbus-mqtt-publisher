using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Services.Types
{
	public interface IRegisterValueFactory
	{
		IRegisterValue Create(RegisterSettings registerSettings);
	}
}