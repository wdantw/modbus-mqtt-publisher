using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services.Types
{
	public interface IRegisterValueFactory
	{
		IRegisterValue Create(RegisterSettings registerSettings);
	}
}