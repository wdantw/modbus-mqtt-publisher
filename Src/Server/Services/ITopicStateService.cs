using ModbusMqttPublisher.Server.Contracts;

namespace ModbusMqttPublisher.Server.Services
{
    public interface ITopicStateService
    {
        TopickStateDto? GetTopicState(string name);
        bool UpdateTopicState(TopickStateDto command);
        void RemoveTopicState(string name);
    }
}