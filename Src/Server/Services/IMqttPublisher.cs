namespace ModbusMqttPublisher.Server.Services
{
    public interface IMqttPublisher
    {
        void PublishTopic(string topicName);
    }
}