using ModbusMqttPublisher.Server.Services.Values;

namespace ModbusMqttPublisher.Server.Services.Publisher
{
	public record PublishCommand(string TopicName, IPublishValueSorage ValueSorage, bool Retain = true);
}
