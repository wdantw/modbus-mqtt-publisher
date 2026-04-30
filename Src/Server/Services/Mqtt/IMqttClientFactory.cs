using MQTTnet.Extensions.ManagedClient;

namespace ModbusMqttPublisher.Server.Services.Mqtt
{
    public interface IMqttClientFactory
	{
		Task<IManagedMqttClient> Create(CancellationToken cancellationToken);
	}
}