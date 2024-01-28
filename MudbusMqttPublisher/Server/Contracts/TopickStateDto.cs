using MudbusMqttPublisher.Server.Services.Types;

namespace MudbusMqttPublisher.Server.Contracts
{
    public record TopickStateDto
    (
        string TopickName,
        IRegisterValue Value,
        DateTime ReadTime
    );
}
