using MQTTnet;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Services.Mqtt;

namespace ModbusMqttPublisher.Server.Services
{
	public class MqttPublisher : BackgroundService, IMqttPublisher
    {
        private AwaiteableQueue<string> pendingTopics = new();
        private readonly ITopicStateService topicStateService;
        private readonly ILogger<MqttPublisher> logger;
        private readonly IMqttClientFactory mqttClientFactory;

		public MqttPublisher(ITopicStateService topicStateService, ILogger<MqttPublisher> logger, IMqttClientFactory mqttClientFactory)
		{
			this.topicStateService = topicStateService;
			this.logger = logger;
			this.mqttClientFactory = mqttClientFactory;
		}

		public void PublishTopic(string topicName)
        {
            pendingTopics.Enqueue(topicName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await SendPending(stoppingToken);
                await pendingTopics.WaitForItems(stoppingToken);
            }
        }

        public async Task SendPending(CancellationToken cancellationToken)
        {
			using var client = await mqttClientFactory.Create(cancellationToken);

            while (pendingTopics.TryDequeue(out var dequeuedName))
            {
                var state = topicStateService.GetTopicState(dequeuedName);
                if (state == null)
                    continue;

                logger.LogInformation($"Публикация информации для топика {dequeuedName} = {state.Value}");


                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(dequeuedName)
                    .WithPayload(state.Value.ToMqtt())
                    .WithRetainFlag(true)
                    .Build();

                await client.PublishAsync(applicationMessage, cancellationToken);
            }
        }

    }
}
