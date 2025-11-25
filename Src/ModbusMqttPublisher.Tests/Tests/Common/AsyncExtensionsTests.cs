using FluentAssertions;
using FluentAssertions.Execution;
using ModbusMqttPublisher.Server.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ModbusMqttPublisher.Tests.Tests.Common
{
    public class AsyncExtensionsTests
    {
        [Fact]
        public async Task AsyncExtensions_SecondTask_Cancelled()
        {
            // arrange

            bool func1Started = false;
            bool func1Finished = false;
            bool func1FinishedSuccess = false;
            bool func2Started = false;
            bool func2Finished = false;
            bool func2FinishedSuccess = false;

            async Task func1(CancellationToken cancellationToken)
            {
                func1Started = true;
                try
                {
                    await Task.Delay(50, cancellationToken);
                }
                finally
                {
                    func1Finished = true;
                }
                func1FinishedSuccess = true;
            }

            async Task func2(CancellationToken cancellationToken)
            {
                func2Started = true;
                try
                {
                    await Task.Delay(1000000, cancellationToken);
                }
                finally
                {
                    func2Finished = true;
                }
                func2FinishedSuccess = true;
            }

            // act
            var winTask = await AsyncExtensions.WhenAnyCancellable(func1, func2, CancellationToken.None);
            await winTask;

            // asserts
            using var _ = new AssertionScope();
            func1Started.Should().BeTrue();
            func1Finished.Should().BeTrue();
            func1FinishedSuccess.Should().BeTrue();
            func2Started.Should().BeTrue();
            func2Finished.Should().BeTrue();
            func2FinishedSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task AsyncExtensions_Cancelled()
        {
            // arrange

            bool func1Started = false;
            bool func1Finished = false;
            bool func1FinishedSuccess = false;
            bool func2Started = false;
            bool func2Finished = false;
            bool func2FinishedSuccess = false;

            async Task func1(CancellationToken cancellationToken)
            {
                func1Started = true;
                try
                {
                    await Task.Delay(1000000, cancellationToken);
                }
                finally
                {
                    func1Finished = true;
                }
                func1FinishedSuccess = true;
            }

            async Task func2(CancellationToken cancellationToken)
            {
                func2Started = true;
                try
                {
                    await Task.Delay(1000000, cancellationToken);
                }
                finally
                {
                    func2Finished = true;
                }
                func2FinishedSuccess = true;
            }

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(50);

            // act
            var winTask = await AsyncExtensions.WhenAnyCancellable(func1, func2, cts.Token);

            // asserts
            using var _ = new AssertionScope();
            await new Func<Task>(() => winTask).Should().ThrowAsync<OperationCanceledException>();
            func1Started.Should().BeTrue();
            func1Finished.Should().BeTrue();
            func1FinishedSuccess.Should().BeFalse();
            func2Started.Should().BeTrue();
            func2Finished.Should().BeTrue();
            func2FinishedSuccess.Should().BeFalse();
        }
    }
}
