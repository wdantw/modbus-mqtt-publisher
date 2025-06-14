
namespace ModbusMqttPublisher.Server.Services.Mqtt
{
    public interface IMqttBus
    {
        Task EnqueueMessage(string fullTopicName, ArraySegment<byte> payload, bool retain, CancellationToken cancellationToken);
    }
}