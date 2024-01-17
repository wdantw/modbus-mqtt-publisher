namespace MudbusMqttPublisher.Server.Contracts
{
    public record TopickStateCommand
    (
        string TopickName,
        object Value,
        DateTime ReadTime
    );
}
