namespace MudbusMqttPublisher.Server.Contracts
{
    public record TopickStateDto
    (
        string TopickName,
        object Value,
        DateTime ReadTime
    );
}
