using Microsoft.Extensions.Options;
using ModbusMqttPublisher.Server.Contracts.Configs;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;

namespace ModbusMqttPublisher.Server.Services.Mqtt
{
	public class MqttClientFactory : IMqttClientFactory
	{
		private readonly IOptions<MqttOptions> _options;
		private readonly MqttFactory _mqttFactory;

        public MqttClientFactory(
            IOptions<MqttOptions> options,
            MqttFactory mqttFactory)
        {
            _options = options;
            _mqttFactory = mqttFactory;
        }

        public async Task<IManagedMqttClient> Create(CancellationToken cancellationToken)
		{
            var options = _options.Value;

            // client option

			var clientOptionsBuilder = _mqttFactory.CreateClientOptionsBuilder();
            if (!string.IsNullOrWhiteSpace(options.TcpAddress))
                clientOptionsBuilder.WithTcpServer(options.TcpAddress);

            // managment client options

            var managmentClientOptions = new ManagedMqttClientOptions();

            if (options.AutoReconnectDelay.HasValue)
                managmentClientOptions.AutoReconnectDelay = options.AutoReconnectDelay.Value;

            if (options.ConnectionCheckInterval.HasValue)
                managmentClientOptions.ConnectionCheckInterval = options.ConnectionCheckInterval.Value;

            managmentClientOptions.ClientOptions = clientOptionsBuilder.Build();
            managmentClientOptions.PendingMessagesOverflowStrategy = MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage;

            // creating client

            var client = _mqttFactory.CreateManagedMqttClient();
            await client.StartAsync(managmentClientOptions);
			return client;
		}
	}
}
