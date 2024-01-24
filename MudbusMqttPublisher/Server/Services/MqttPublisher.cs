using Microsoft.Extensions.Options;
using MQTTnet;
using MudbusMqttPublisher.Server.Common;
using MudbusMqttPublisher.Server.Contracts.Configs;

namespace MudbusMqttPublisher.Server.Services
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

                logger.LogInformation($"Публикация информации для топика {dequeuedName}");


                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(dequeuedName)
                    .WithPayload(state.Value.ToString())
                    .WithRetainFlag(true)
                    .Build();

                await client.PublishAsync(applicationMessage, cancellationToken);
            }
        }

    }
}
