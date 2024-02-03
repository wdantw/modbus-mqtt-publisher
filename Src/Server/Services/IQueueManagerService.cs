namespace ModbusMqttPublisher.Server.Services
{
    public interface IQueueManagerService
    {
        string? GetTopicSerialName(string topicName);
        void ReloadSettings();
    }
}