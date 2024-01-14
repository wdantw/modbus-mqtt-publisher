namespace MudbusMqttPublisher.Server.Contracts
{
    public record PublishItem
    (
        string TopickName,
        object Value
    );
}
