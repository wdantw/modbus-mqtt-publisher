using FluentAssertions;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Domain;
using NSubstitute;
using System;
using Xunit;

namespace ModbusMqttPublisher.Tests.Tests.Domain
{
    public class RegisterTests
    {
        [Theory]
        // все регистры ни разу ни читались
        [InlineData(null, null, null, null, null)]
        [InlineData(1, null, null, null, true)]
        [InlineData(1, 10, null, null, true)]

        // все регистры не переодические
        [InlineData(null, null, "12:00:00", null, false)]
        [InlineData(null, null, "12:00:00", "12:00:01", null)]

        // один переодический и один читался
        [InlineData(1, null, "12:00:00", null, false)]
        [InlineData(1, null, null, "12:00:00", true)]

        // оба переодичских и один читался
        [InlineData(1, 10, "12:00:00", null, false)]
        [InlineData(10, 1, "12:00:00", null, false)]

        // один переодический и оба читались
        [InlineData(1, null, "12:01:00", "12:00:00", true)]
        [InlineData(1, null, "12:00:00", "12:01:00", true)]

        // оба переодических и оба читались
        [InlineData(1, 10, "12:01:00", "12:00:00", false)]
        [InlineData(1, 10, "12:00:00", "12:01:00", true)]
        [InlineData(10, 1, "12:01:00", "12:00:00", false)]
        [InlineData(10, 1, "12:00:00", "12:01:00", true)]

        // оба переодических и оба читались - варианты с точной подгоной времени
        [InlineData(5, 10, "12:00:03", "12:00:00", true)]
        [InlineData(5, 10, "12:00:07", "12:00:00", false)]

        // оба переодических и оба читались неопределенный случай
        [InlineData(1, 1, "12:00:00", "12:00:00", null)]
        [InlineData(5, 10, "12:00:05", "12:00:00", null)]

        public void RegisterPriority(
            int? period1,
            int? period2,
            string? lastRead1,
            string? lastRead2,
            bool? firstHasPriority)
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted()
            {
                Number = 1,
                ReadPeriod = period1.HasValue ? TimeSpan.FromSeconds(period1.Value) : null,
                RegType = ConfigRegisterType.HoldingRegister,
            };

            var settings2 = new ModbusRegisterCompleted()
            {
                Number = 2,
                ReadPeriod = period2.HasValue ? TimeSpan.FromSeconds(period2.Value) : null,
                RegType = ConfigRegisterType.HoldingRegister,
            };

            var register1 = new ReadRegister(settings1, Substitute.For<IReadPriorityCallbacks<ReadRegister>>());
            var register2 = new ReadRegister(settings2, Substitute.For<IReadPriorityCallbacks<ReadRegister>>());

            var testDate = new DateTime(2000, 10, 10);

            if (lastRead1 != null)
                register1.ValueReadedFromDevice(testDate + TimeSpan.Parse(lastRead1));

            if (lastRead2 != null)
                register2.ValueReadedFromDevice(testDate + TimeSpan.Parse(lastRead2));

            // act
            var calulatedPriority = register1.HasMorePriorityForRead(register2);
            var inversedCalcPriority = register2.HasMorePriorityForRead(register1);

            // assert
            if (!firstHasPriority.HasValue)
            {
                calulatedPriority.Should().Be(inversedCalcPriority);
            }
            else
            {
                calulatedPriority.Should().Be(firstHasPriority.Value);
                inversedCalcPriority.Should().Be(!firstHasPriority.Value);
            }
        }

        [Fact]
        public void RegisterPriorityUp()
        {

            // arrange
            var settings = new ModbusRegisterCompleted()
            {
                Number = 1,
                RegType = ConfigRegisterType.HoldingRegister,
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegister>>();

            var register = new ReadRegister(settings, callbacks);

            // act
            register.ValueWritedToDevice(DateTime.Now);

            // assert
            callbacks.Received(1).ChildItemPriorityUp(Arg.Any<ReadRegister>(), Arg.Any<DateTime>());
            callbacks.Received(0).ChildItemPriorityDown(Arg.Any<ReadRegister>(), Arg.Any<DateTime>());
            callbacks.Received(0).ChildItemAccessFailed(Arg.Any<ReadRegister>(), Arg.Any<DateTime>());
        }


        [Fact]
        public void RegisterPriorityDown()
        {

            // arrange
            var settings = new ModbusRegisterCompleted()
            {
                Number = 1,
                RegType = ConfigRegisterType.HoldingRegister,
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegister>>();

            var register = new ReadRegister(settings, callbacks);

            // act
            register.ValueReadedFromDevice(DateTime.Now);

            // assert
            callbacks.Received(0).ChildItemPriorityUp(Arg.Any<ReadRegister>(), Arg.Any<DateTime>());
            callbacks.Received(1).ChildItemPriorityDown(Arg.Any<ReadRegister>(), Arg.Any<DateTime>());
            callbacks.Received(0).ChildItemAccessFailed(Arg.Any<ReadRegister>(), Arg.Any<DateTime>());
        }
    }
}
