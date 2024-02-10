using MQTTnet.Client;
using Microsoft.Extensions.Options;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Services.Mqtt;

namespace ModbusMqttPublisher.Server.Services
{
	public class MqttConsumer : BackgroundService
    {
        private readonly IOptions<MqttOptions> options;
        private readonly IWriteQueueService writeQueueService;
        private readonly IQueueManagerService queueManagerService;
		private readonly IMqttClientFactory mqttClientFactory;

		public MqttConsumer(IOptions<MqttOptions> options, IWriteQueueService writeQueueService, IQueueManagerService queueManagerService, IMqttClientFactory mqttClientFactory)
		{
			this.options = options;
			this.writeQueueService = writeQueueService;
			this.queueManagerService = queueManagerService;
			this.mqttClientFactory = mqttClientFactory;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
			using var client = await mqttClientFactory.Create(stoppingToken);

            client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;

            var mqttSubscribeOptions = new MqttClientSubscribeOptionsBuilder()
				.WithTopicFilter(f => f.WithTopic(MqttPath.CombineTopicPath(options.Value.BaseTopicPath, "#")))
                .Build();

            await client.SubscribeAsync(mqttSubscribeOptions, stoppingToken);
			await stoppingToken.WhenCancelled();
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
