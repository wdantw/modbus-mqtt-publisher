using System.Buffers;

namespace ModbusMqttPublisher.Server.Contracts
{
	public record WriteQuery(string TopicName, ReadOnlySequence<byte> Value);
}
