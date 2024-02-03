using MQTTnet;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Services.Mqtt;
using MQTTnet.Client;

namespace ModbusMqttPublisher.Server.Services.Publisher
{
	public class MqttPublisher : BackgroundService, IMqttPublisher
    {
        private AwaiteableQueue<PublishCommand> pendingTopics = new();
        private readonly ILogger<MqttPublisher> logger;
        private readonly IMqttClientFactory mqttClientFactory;

        public MqttPublisher(ILogger<MqttPublisher> logger, IMqttClientFactory mqttClientFactory)
        {
            this.logger = logger;
            this.mqttClientFactory = mqttClientFactory;
        }

        public void PublishTopic(PublishCommand command)
        {
            pendingTopics.Enqueue(command);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
			using var client = await mqttClientFactory.Create(stoppingToken);

			while (!stoppingToken.IsCancellationRequested)
            {
                await SendPending(client, stoppingToken);
                await pendingTopics.WaitForItems(stoppingToken);
            }
        }

        public async Task SendPending(IMqttClient client, CancellationToken cancellationToken)
        {
            while (pendingTopics.TryDequeue(out var command))
            {
                logger.LogInformation($"Публикация информации для топика {command.TopicName} = {command.ValueSorage}");

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(command.TopicName)
                    .WithPayload(command.ValueSorage.ToMqtt())
                    .WithRetainFlag(command.Retain)
                    .Build();

                await client.PublishAsync(applicationMessage, cancellationToken);
            }
        }

    }
}
