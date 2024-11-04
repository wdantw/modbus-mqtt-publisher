using AutoFixture;
using FluentAssertions;
using ModbusMqttPublisher.Tests.Common;
using ModbusMqttPublisher.Tests.Hosts;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ModbusMqttPublisher.Tests.Tests.Mqtt
{
    [Collection("MqttTestHostCollection")]
    public class MqttBusShutdownTest : IClassFixture<MqttTestHost>
    {
        private readonly MqttTestHost _host;

        public MqttBusShutdownTest(MqttTestHost host)
        {
            _host = host;
        }

        [Fact]
        public async Task ProduceMessagesOnShutdownSuccess()
        {
            // arrange
            var fixture = new Fixture();
            var messagesCount = 10;
            var topicNamePreffix = "test5/";
            var messages = Enumerable.Range(0, messagesCount).Select(i => fixture.Create<string>()).ToArray();
            var cancellationToken = Utils.CreateCancellationToken(10000);

            var consumeTasks = messages.Select(m => _host.ConsumeMessageWithRawClient(topicNamePreffix + m, m, cancellationToken)).ToArray();

            // act
            await Task.WhenAll(messages.Select(m => _host.EnqueMessageToMqttBus(topicNamePreffix + m, m, cancellationToken)));
            await _host.DisposeAsync();
            await Task.WhenAll(consumeTasks);

            // assert
            consumeTasks.Select(t => t.IsCompletedSuccessfully).ToArray().Should().AllBeEquivalentTo(true);

        }
    }
}
