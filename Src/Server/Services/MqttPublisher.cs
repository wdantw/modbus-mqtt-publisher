using Microsoft.Extensions.Options;
using MQTTnet;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts.Configs;

namespace ModbusMqttPublisher.Server.Services
{
    public class MqttPublisher : BackgroundService, IMqttPublisher
    {
        private readonly IOptions<MqttOptions> options;
        private AwaiteableQueue<string> pendingTopics = new();
        private readonly ITopicStateService topicStateService;
        private readonly ILogger<MqttPublisher> logger;

        public MqttPublisher(IOptions<MqttOptions> options, ITopicStateService topicStateService, ILogger<MqttPublisher> logger)
        {
            this.options = options;
            this.topicStateService = topicStateService;
            this.logger = logger;
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
            var mqttFactory = new MqttFactory();
            using var client = mqttFactory.CreateMqttClient();
            var connectOptions = mqttFactory.CreateClientOptionsBuilder()
                .WithTcpServer(options.Value.TcpAddress)
                .Build();

            await client.ConnectAsync(connectOptions, cancellationToken);

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
