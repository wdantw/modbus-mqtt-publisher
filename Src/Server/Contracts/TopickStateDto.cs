using ModbusMqttPublisher.Server.Services.Types;

namespace ModbusMqttPublisher.Server.Contracts
{
    public record TopickStateDto
    (
        string TopickName,
		IPublishValue Value,
        DateTime ReadTime
    );
}
