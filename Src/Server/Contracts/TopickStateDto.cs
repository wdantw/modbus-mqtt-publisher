using MudbusMqttPublisher.Server.Services.Types;

namespace MudbusMqttPublisher.Server.Contracts
{
    public record TopickStateDto
    (
        string TopickName,
		IPublishValue Value,
        DateTime ReadTime
    );
}
