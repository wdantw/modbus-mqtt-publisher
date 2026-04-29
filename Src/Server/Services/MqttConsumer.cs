using Microsoft.Extensions.Options;
using ModbusMqttPublisher.Server.Contracts.Configs;
using MQTTnet;
using MQTTnet.DependencyInjection;

namespace ModbusMqttPublisher.Server.Services
{
    public class MqttConsumer : IMqttConsumer
    {
        private readonly IWriteQueueService writeQueueService;
        private readonly IQueueManagerService queueManagerService;
        private readonly IOptions<PublisherMqttOptions> _options;
        private readonly ILogger<MqttConsumer> _logger;

        public MqttConsumer(IWriteQueueService writeQueueService, IQueueManagerService queueManagerService, IOptions<PublisherMqttOptions> options, ILogger<MqttConsumer> logger)
        {
            this.writeQueueService = writeQueueService;
            this.queueManagerService = queueManagerService;
            _options = options;
            _logger = logger;
        }

        public Task Handle(MqttApplicationMessage message, CancellationToken cancellationToken)
        {
            var topicName = message.Topic;
            var baseTopicName = _options.Value.BaseTopicPath;
            var relativeTopicName = MqttPath.GetRelativeTopicName(topicName, baseTopicName);

            if (relativeTopicName == null)
            {
                _logger.LogWarning("Пришло сообщение из топика {topicName} не соответсвующее настроенному базовому пути {baseTopicName}", topicName, baseTopicName);
                return Task.CompletedTask;
            }

            _logger.LogDebug("Принято сообщение и MQTT топика {topicName}", topicName);

            var serialName = queueManagerService.GetTopicSerialName(relativeTopicName);
            if (serialName == null)
                return Task.CompletedTask;

            writeQueueService.AddWriteRequest(serialName, relativeTopicName, message.Payload);
            return Task.CompletedTask;
        }
    }
}
