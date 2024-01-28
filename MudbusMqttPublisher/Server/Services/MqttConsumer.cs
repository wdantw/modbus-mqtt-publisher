using MQTTnet.Client;
using MQTTnet;
using Microsoft.Extensions.Options;
using MudbusMqttPublisher.Server.Contracts.Configs;
using MudbusMqttPublisher.Server.Common;

namespace MudbusMqttPublisher.Server.Services
{
	public class MqttConsumer : BackgroundService
    {
        private readonly IOptions<MqttOptions> options;
        private readonly IWriteQueueService writeQueueService;
        private readonly IQueueManagerService queueManagerService;

        public MqttConsumer(IOptions<MqttOptions> options, IWriteQueueService writeQueueService, IQueueManagerService queueManagerService)
        {
            this.options = options;
            this.writeQueueService = writeQueueService;
            this.queueManagerService = queueManagerService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mqttFactory = new MqttFactory();
            using var client = mqttFactory.CreateMqttClient();
            var connectOptions = mqttFactory.CreateClientOptionsBuilder()
                .WithTcpServer(options.Value.TcpAddress)
                .Build();

            client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;
            await client.ConnectAsync(connectOptions, stoppingToken);
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(MqttPath.CombineTopicPath(options.Value.BaseTopicPath, "#")))
                .Build();

            await client.SubscribeAsync(mqttSubscribeOptions, stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            if (!arg.ApplicationMessage.Topic.StartsWith(options.Value.BaseTopicPath + MqttPath.TopicPathDelimeter))
                return Task.CompletedTask;

            var name = arg.ApplicationMessage.Topic[(options.Value.BaseTopicPath.Length + 1)..];

            var serialName = queueManagerService.GetTopicSerialName(name);

            if (serialName == null)
                return Task.CompletedTask;

            writeQueueService.AddWriteRequest(serialName, name, arg.ApplicationMessage.PayloadSegment);

            return Task.CompletedTask;
        }
    }
}
