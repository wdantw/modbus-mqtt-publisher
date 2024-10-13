using Microsoft.Extensions.DependencyInjection;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Infrastructure;
using ModbusMqttPublisher.Server.Services.Mqtt;
using ModbusMqttPublisher.Tests.Common;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading.Tasks;
using System.Threading;
using ModbusMqttPublisher.Server.Common;
using System.Text;
using MQTTnet.Protocol;
using NSubstitute;

namespace ModbusMqttPublisher.Tests.Hosts
{
    public class MqttTestHost : BaseTestHost
    {
        public const string BaseTopicPath = "test";

        public IMqttClientFactory MqttClientFactory => Host.Services.GetRequiredService<IMqttClientFactory>();
        public IMqttBus MqttBus => Host.Services.GetRequiredService<IMqttBus>();
        public IMqttConsumer FakeMqttConsumer { get; private set; } = null!;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddMqtt();
            services.Configure<MqttOptions>(opt =>
            {
                opt.TcpAddress = "localhost";
                opt.AutoReconnectDelay = TimeSpan.FromMilliseconds(100);
                opt.ConnectionCheckInterval = TimeSpan.FromMilliseconds(100);
                opt.BaseTopicPath = BaseTopicPath;
            });
            FakeMqttConsumer = services.AddFakeService<IMqttConsumer>();
        }

        public override async Task OnHostStartedAsync(CancellationToken cancellationToken)
        {
            await MosquittoServiceUtils.StartMosquitto(cancellationToken);
        }

        // соответсвует внутренним преобрзованиям MQTTNet
        public static byte[] EncodePayload(string message)
            => Encoding.UTF8.GetBytes(message);

        public async Task PublishMessageWithRawClient(string relativeTopicName, string payload, bool retain, CancellationToken cancellationToken = default)
        {
            using var mqttClient = await MqttClientFactory.Create(cancellationToken).ConfigureAwait(false);

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(MqttPath.CombineTopicPath(BaseTopicPath, relativeTopicName))
                .WithPayload(payload)
                .WithRetainFlag(retain)
                .Build();

            await mqttClient.EnqueueAsync(applicationMessage).WithCancellation(cancellationToken).ConfigureAwait(false);

            await Utils.SpinUntil(() => mqttClient.PendingApplicationMessagesCount == 0, cancellationToken).ConfigureAwait(false);

            await mqttClient.StopAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
        }

        public async Task ConsumeMessageWithRawClient(string relativeTopicName, string payload, CancellationToken cancellationToken = default)
        {
            using var mqttClient = await MqttClientFactory.Create(cancellationToken).ConfigureAwait(false);
            var tcs = new TaskCompletionSource();

            mqttClient.ApplicationMessageReceivedAsync += args => {
                
                var receivedTopic = MqttPath.GetRelativeTopicName(args.ApplicationMessage.Topic, BaseTopicPath);
                var receivedPayload = args.ApplicationMessage.ConvertPayloadToString();

                if (receivedTopic == relativeTopicName && receivedPayload == payload)
                    tcs.TrySetResult();

                return Task.CompletedTask;
            };

            await mqttClient.SubscribeAsync(MqttPath.CombineTopicPath(BaseTopicPath, MqttPath.WildcardSingleLevel), MqttQualityOfServiceLevel.AtLeastOnce);

            await tcs.Task.WithCancellation(cancellationToken).ConfigureAwait(false);
            await mqttClient.StopAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
        }

        public async Task FakeConsumerConsume(string relativeTopicName, string payload, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource();
            FakeMqttConsumer
                .Consume(
                    Arg.Is<string>(x => x == relativeTopicName),
                    Arg.Is<ArraySegment<byte>>(x => Encoding.UTF8.GetString(x) == payload),
                    Arg.Any<CancellationToken>()
                    )
                .Returns(Task.CompletedTask)
                .AndDoes(x => tcs.TrySetResult());

            await tcs.Task.WithCancellation(cancellationToken);
        }
    }
}
