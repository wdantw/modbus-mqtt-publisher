using FluentAssertions;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Domain;
using NSubstitute;
using System;
using System.Linq;
using Xunit;

namespace ModbusMqttPublisher.Tests.Tests.Domain
{
    public class ReadRegisterGroupTests
    {
        [Fact]
        public void TestStartPriority()
        {
            // arrange
            var settings1= new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.Coil },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(3), RegType = ConfigRegisterType.Coil }
            };

            var upCallback = Substitute.For<PriorityChangedDelegate<ReadRegisterGroup>>();
            var downCallback = Substitute.For<PriorityChangedDelegate<ReadRegisterGroup>>();
            var group1 = new ReadRegisterGroup(
                regSettings: settings1,
                priorityUpCallback: upCallback,
                priorityDownCallback: downCallback);

            // act
            var mostPriority = group1.EnsureMostPrioriyItem();

            // arrange
            mostPriority.StartNumber.Should().Be(2);
            upCallback.Received(0);
            downCallback.Received(0);
        }

        [Fact]
        public void TestDownPriority()
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.Coil },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(3), RegType = ConfigRegisterType.Coil }
            };

            var upCallback = Substitute.For<PriorityChangedDelegate<ReadRegisterGroup>>();
            var downCallback = Substitute.For<PriorityChangedDelegate<ReadRegisterGroup>>();
            var group1 = new ReadRegisterGroup(
                regSettings: settings1,
                priorityUpCallback: upCallback,
                priorityDownCallback: downCallback);

            var currentTime = DateTime.Now;

            // act
            group1.Registers.Single(r => r.StartNumber == 2).ValueReadedFromDevice(currentTime);
            var mostPriority = group1.EnsureMostPrioriyItem();

            // arrange
            mostPriority.StartNumber.Should().Be(3);
            upCallback.Received(0);
            downCallback.Received(1);
            downCallback.Received(1)(group1);
        }


        [Fact]
        public void TestUpPriority()
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.Coil },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(3), RegType = ConfigRegisterType.Coil }
            };

            var upCallback = Substitute.For<PriorityChangedDelegate<ReadRegisterGroup>>();
            var downCallback = Substitute.For<PriorityChangedDelegate<ReadRegisterGroup>>();
            var group1 = new ReadRegisterGroup(
                regSettings: settings1,
                priorityUpCallback: upCallback,
                priorityDownCallback: downCallback);

            var currentTime = DateTime.Now;

            foreach(var r in group1.Registers) r.ValueReadedFromDevice(currentTime);

            upCallback.ClearReceivedCalls();
            upCallback.ClearReceivedCalls();

            // act
            group1.Registers.Single(r => r.StartNumber == 4).ValueWritedToDevice(currentTime.AddSeconds(1));
            var mostPriority = group1.EnsureMostPrioriyItem();

            // arrange
            mostPriority.StartNumber.Should().Be(4);
            upCallback.Received(1);
            upCallback.Received(1)(group1);
            downCallback.Received(0);
        }
    }
}
