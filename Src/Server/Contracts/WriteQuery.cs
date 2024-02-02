namespace MudbusMqttPublisher.Server.Contracts
{
	public record WriteQuery(string TopicName, ArraySegment<byte> Value);
}
