namespace ModbusMqttPublisher.Server.Services.Publisher
{
    public interface IMqttPublisher
    {
        void PublishTopic(PublishCommand command);
    }
}