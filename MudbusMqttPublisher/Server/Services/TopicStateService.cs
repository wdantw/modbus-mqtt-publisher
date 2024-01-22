using Microsoft.Extensions.Options;
using MQTTnet;
using MudbusMqttPublisher.Server.Contracts;
using MudbusMqttPublisher.Server.Contracts.Configs;
using System.Collections.Concurrent;

namespace MudbusMqttPublisher.Server.Services
{
    public class TopicStateService : ITopicStateService
    {
        private class TopickState
        {
            public string Name { get; }
            public object Value { get; set; }
            public DateTime LastReadTime { get; set; }
            public DateTime LastUpdateTime { get; set; }

            public TopickState(string name, object value, DateTime time)
            {
                Name = name;
                Value = value;
                LastReadTime = time;
                LastUpdateTime = time;
            }

            public bool UpdateCommand(TopickStateDto updateCommand, DateTime readTime)
            {
                if (updateCommand.TopickName != Name)
                    throw new Exception("Имя топика не совпадает с переданной командой");

                var changed = !Equals(Value, updateCommand.Value);

                Value = updateCommand.Value;
                LastReadTime = readTime;
                if (changed) LastUpdateTime = readTime;

                return changed;
            }
        }

        private readonly IOptions<MqttOptions> options;
        private ConcurrentDictionary<string, TopickState> topickStates = new();

        public TopicStateService(IOptions<MqttOptions> options)
        {
            this.options = options;
        }

        public bool UpdateTopicState(TopickStateDto command)
        {
            bool isNew = false;
            var state = topickStates.GetOrAdd(command.TopickName, _ => { isNew = true; return new TopickState(command.TopickName, command.Value, command.ReadTime);  });
            if (isNew) return true;
            return state.UpdateCommand(command, command.ReadTime);
        }

        public TopickStateDto? GetTopicState(string name)
        {
            if (!topickStates.TryGetValue(name, out var state))
                return null;

            return new TopickStateDto(name, state.Value, state.LastReadTime);
        }

    }
}
