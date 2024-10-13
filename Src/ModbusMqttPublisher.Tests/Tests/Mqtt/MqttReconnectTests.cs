using AutoFixture;
using FluentAssertions;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Tests.Hosts;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ModbusMqttPublisher.Tests.Tests.Mqtt
{
    [Collection(nameof(MqttTestHostCollection))]
    public class MqttReconnectTests
    {
        private readonly MqttTestHost _host;

        public MqttReconnectTests(MqttTestHost host)
        {
            _host = host;
        }

        private async Task PublishMessage(string topic, string playload, CancellationToken cancellationToken)
        {
            using var mqttClient = await _host.MqttClientFactory.Create(cancellationToken);

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(playload)
                .WithRetainFlag(true)
                .Build();

            await mqttClient.EnqueueAsync(applicationMessage);

            await Utils.SpinUntil(() => mqttClient.PendingApplicationMessagesCount == 0, cancellationToken);
        }

        [Fact]
        public async Task LongRuningTest()
        {
            // arrange
            var topic1 = "test/test1";
            var topic2 = "test/test2";
            var messageText = new Fixture().Create<string>();

            var cts = new CancellationTokenSource();
            var tcs1 = new TaskCompletionSource();
            var tcs2 = new TaskCompletionSource();
            cts.CancelAfter(10000);
            var cancellationToken = cts.Token;

            Task receviedDelegate(MqttApplicationMessageReceivedEventArgs e)
            {
                var topic = e.ApplicationMessage.Topic;
                var playload = Encoding.ASCII.GetString(e.ApplicationMessage.PayloadSegment);

                if (topic == topic1 && playload == messageText)
                    tcs1.TrySetResult();

                if (topic == topic2 && playload == messageText)
                    tcs2.TrySetResult();

                return Task.CompletedTask;
            }

            using var mqttClient = await _host.MqttClientFactory.Create(cancellationToken);

            mqttClient.ApplicationMessageReceivedAsync += receviedDelegate;
            await mqttClient.SubscribeAsync("test/#");

            // act
            await Utils.StartMosquitto(cancellationToken);
            await PublishMessage(topic1, messageText, cancellationToken);
            await tcs1.Task.WithCancellation(cancellationToken);
            await Utils.RestartMosquitto(cancellationToken);
            await PublishMessage(topic2, messageText, cancellationToken);
            await tcs2.Task.WithCancellation(cancellationToken);

            // assert
            tcs1.Task.IsCompletedSuccessfully.Should().Be(true);
            tcs2.Task.IsCompletedSuccessfully.Should().Be(true);
        }
    }
}
