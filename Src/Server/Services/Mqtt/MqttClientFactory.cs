using Microsoft.Extensions.Options;
using ModbusMqttPublisher.Server.Contracts.Configs;
using MQTTnet;
using MQTTnet.Client;

namespace ModbusMqttPublisher.Server.Services.Mqtt
{
	public class MqttClientFactory : IMqttClientFactory
	{
		private readonly IOptions<MqttOptions> options;

		public MqttClientFactory(IOptions<MqttOptions> options)
		{
			this.options = options;
		}

		public async Task<IMqttClient> Create(CancellationToken cancellationToken)
		{
			var mqttFactory = new MqttFactory();

			var client = mqttFactory.CreateMqttClient();

			var connectOptions = mqttFactory.CreateClientOptionsBuilder()
				.WithTcpServer(options.Value.TcpAddress)
				.Build();

			await client.ConnectAsync(connectOptions, cancellationToken);

			return client;
		}
	}
}
