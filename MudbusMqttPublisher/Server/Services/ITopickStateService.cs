using MudbusMqttPublisher.Server.Contracts;

namespace MudbusMqttPublisher.Server.Services
{
    public interface ITopickStateService
    {
        TopickStateCommand? GetTopickState(string name);
        void UpdateTopickState(TopickStateCommand command);
    }
}