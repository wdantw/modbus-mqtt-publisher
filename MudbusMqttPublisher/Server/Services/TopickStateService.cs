using Microsoft.Extensions.Options;
using MQTTnet;
using MudbusMqttPublisher.Server.Contracts;
using System.Collections.Concurrent;

namespace MudbusMqttPublisher.Server.Services
{
    public class TopickStateService : BackgroundService, ITopickStateService
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

            public bool UpdateCommand(TopickStateCommand updateCommand, DateTime readTime)
            {
                if (updateCommand.TopickName != Name)
                    throw new Exception("Имя топика не совпадает с переданной командой");

                var changed = Equals(Value, updateCommand.Value);

                Value = updateCommand.Value;
                LastReadTime = readTime;
                if (changed) LastUpdateTime = readTime;

                return changed;
            }

        }

        private readonly IOptions<MqttOptions> options;
        private ConcurrentDictionary<string, TopickState> topickStates = new();
        private ConcurrentQueue<string> pendingTopiks = new();
        private volatile TaskCompletionSource hasQueueTsc = new();

        public TopickStateService(IOptions<MqttOptions> options)
        {
            this.options = options;
        }

        public void UpdateTopickState(TopickStateCommand command)
        {
            var state = topickStates.GetOrAdd(command.TopickName, new TopickState(command.TopickName, command.Value, command.ReadTime));

            if (state.UpdateCommand(command, command.ReadTime))
            {
                pendingTopiks.Append(command.TopickName);
                hasQueueTsc.TrySetResult();
            }
        }

        public TopickStateCommand? GetTopickState(string name)
        {
            if (!topickStates.TryGetValue(name, out var state))
                return null;

            return new TopickStateCommand(name, state.Value, state.LastReadTime);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                await SendPending(stoppingToken);

                hasQueueTsc = new TaskCompletionSource();

                if (!pendingTopiks.IsEmpty)
                    continue;

                await hasQueueTsc.Task;
            }
        }

        public async Task SendPending(CancellationToken cancellationToken)
        {
            var mqttFactory = new MqttFactory();
            using var client = mqttFactory.CreateMqttClient();
            var connectOptions = mqttFactory.CreateClientOptionsBuilder()
                .WithTcpServer(options.Value.TcpAddress)
                .Build();

            await client.ConnectAsync(connectOptions);

            while (pendingTopiks.TryDequeue(out var dequeuedName))
            {
                if (!topickStates.TryGetValue(dequeuedName, out var state))
                    continue;

                var value = state.Value;

                if (value == null)
                    continue;

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(dequeuedName)
                    .WithPayload(value.ToString())
                    .WithRetainFlag(true)
                    .Build();

                await client.PublishAsync(applicationMessage, cancellationToken);
            }

        }
    }
}
