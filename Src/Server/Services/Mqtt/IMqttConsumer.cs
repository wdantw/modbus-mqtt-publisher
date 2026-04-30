namespace ModbusMqttPublisher.Server.Services.Mqtt
{
    public interface IMqttConsumer
    {
        Task Consume(string relativeTopicName, ArraySegment<byte> payload, CancellationToken cancellationToken);
    }
}
