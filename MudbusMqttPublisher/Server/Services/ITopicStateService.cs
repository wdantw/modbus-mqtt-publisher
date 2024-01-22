using MudbusMqttPublisher.Server.Contracts;

namespace MudbusMqttPublisher.Server.Services
{
    public interface ITopicStateService
    {
        TopickStateCommand? GetTopicState(string name);
        bool UpdateTopicState(TopickStateCommand command);
    }
}