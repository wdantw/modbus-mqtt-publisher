namespace MudbusMqttPublisher.Server.Contracts
{
    public record WriteQuery(string TopicName, object Value);
}
