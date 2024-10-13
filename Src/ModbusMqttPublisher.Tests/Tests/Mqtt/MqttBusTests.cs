using AutoFixture;
using FluentAssertions;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Tests.Common;
using ModbusMqttPublisher.Tests.Hosts;
using System.Threading.Tasks;
using Xunit;

namespace ModbusMqttPublisher.Tests.Tests.Mqtt
{
    [Collection(nameof(MqttTestHostCollection))]
    public class MqttBusTests
    {
        private readonly MqttTestHost _host;

        public MqttBusTests(MqttTestHost host)
        {
            _host = host;
        }

        [Fact]
        public async Task MosquittoRestartTest()
        {
            // arrange
            var topic1 = "test1";
            var topic2 = "test2";
            var messageText1 = new Fixture().Create<string>();
            var messageText2 = new Fixture().Create<string>();

            var cancellationToken = Utils.CreateCancellationToken(5000);
            var cosumeTask1 = _host.FakeConsumerConsume(topic1, messageText1, cancellationToken);
            var cosumeTask2 = _host.FakeConsumerConsume(topic2, messageText2, cancellationToken);

            // act
            await _host.PublishMessageWithRawClient(topic1, messageText1, true, cancellationToken);
            await cosumeTask1;

            await MosquittoServiceUtils.RestartMosquitto(cancellationToken);

            await _host.PublishMessageWithRawClient(topic2, messageText2, true, cancellationToken);
            await cosumeTask2;

            // assert
            cosumeTask1.IsCompletedSuccessfully.Should().BeTrue();
            cosumeTask2.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Fact]
        public async Task ReceiveSuccess()
        {
            // arrange
            var message = new Fixture().Create<string>();
            var topicName = "test3";
            var cancellationToken = Utils.CreateCancellationToken(1000);
            
            var consumeTask = _host.FakeConsumerConsume(topicName, message, cancellationToken);

            // act
            await _host.PublishMessageWithRawClient(topicName, message, true, cancellationToken);
            await consumeTask;

            // assert
            consumeTask.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Fact]
        public async Task ProduceSuccess()
        {
            // arrange
            var message = new Fixture().Create<string>();
            var topicName = "test4";
            var cancellationToken = Utils.CreateCancellationToken(1000);

            var consumeTask = _host.ConsumeMessageWithRawClient(topicName, message, cancellationToken);

            // act
            await _host.MqttBus.EnqueueMessage(
                MqttPath.CombineTopicPath(MqttTestHost.BaseTopicPath, topicName),
                MqttTestHost.EncodePayload(message),
                true,
                cancellationToken);
            await consumeTask;

            // assert
            consumeTask.IsCompletedSuccessfully.Should().BeTrue();
        }
    }
}
