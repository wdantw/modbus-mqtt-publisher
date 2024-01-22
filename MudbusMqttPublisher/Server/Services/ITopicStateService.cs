using MudbusMqttPublisher.Server.Contracts;

namespace MudbusMqttPublisher.Server.Services
{
    public interface ITopicStateService
    {
        TopickStateDto? GetTopicState(string name);
        bool UpdateTopicState(TopickStateDto command);
    }
}