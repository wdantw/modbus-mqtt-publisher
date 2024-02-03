using MQTTnet.Client;

namespace ModbusMqttPublisher.Server.Services.Mqtt
{
	public interface IMqttClientFactory
	{
		Task<IMqttClient> Create(CancellationToken cancellationToken);
	}
}