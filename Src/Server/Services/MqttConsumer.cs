using ModbusMqttPublisher.Server.Services.Mqtt;

namespace ModbusMqttPublisher.Server.Services
{
    public class MqttConsumer : IMqttConsumer
    {
        private readonly IWriteQueueService writeQueueService;
        private readonly IQueueManagerService queueManagerService;

        public MqttConsumer(IWriteQueueService writeQueueService, IQueueManagerService queueManagerService)
        {
            this.writeQueueService = writeQueueService;
            this.queueManagerService = queueManagerService;
        }

        public Task Consume(string relativeTopicName, ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            var serialName = queueManagerService.GetTopicSerialName(relativeTopicName);

            if (serialName == null)
                return Task.CompletedTask;

            writeQueueService.AddWriteRequest(serialName, relativeTopicName, payload);

            return Task.CompletedTask;
        }
    }
}
