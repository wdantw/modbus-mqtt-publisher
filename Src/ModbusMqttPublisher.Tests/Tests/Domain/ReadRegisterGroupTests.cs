using FluentAssertions;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Contracts.Configs.Enums;
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
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister, Name = "1" },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister, Name = "2" },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.HoldingRegister, Name = "3" },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(3), RegType = ConfigRegisterType.HoldingRegister, Name = "4" }
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegisterGroup>>();

            var group1 = new ReadRegisterGroup(settings1, callbacks, "");

            // act
            var mostPriority = group1.EnsureMostPrioriyItem();

            // arrange
            mostPriority.StartNumber.Should().Be(2);
            callbacks.Received(0).ChildItemPriorityUp(Arg.Any<ReadRegisterGroup>(), Arg.Any<DateTime>());
            callbacks.Received(0).ChildItemPriorityDown(Arg.Any<ReadRegisterGroup>(), Arg.Any<DateTime>());
            callbacks.Received(0).ChildItemAccessFailed(Arg.Any<ReadRegisterGroup>(), Arg.Any<DateTime>());
        }

        [Fact]
        public void TestDownPriority()
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister, Name = "1" },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister, Name = "2" },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.HoldingRegister, Name = "3" },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(3), RegType = ConfigRegisterType.HoldingRegister, Name = "4" }
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegisterGroup>>();

            var group1 = new ReadRegisterGroup(settings1, callbacks, "");

            var currentTime = DateTime.Now;

            // act
            group1.Registers.Single(r => r.StartNumber == 2).ValueReadedFromDevice(currentTime);
            var mostPriority = group1.EnsureMostPrioriyItem();

            // arrange
            mostPriority.StartNumber.Should().Be(3);
            callbacks.Received(0).ChildItemPriorityUp(Arg.Any<ReadRegisterGroup>(), Arg.Any<DateTime>());
            callbacks.Received(1).ChildItemPriorityDown(Arg.Any<ReadRegisterGroup>(), Arg.Any<DateTime>());
            callbacks.Received(1).ChildItemPriorityDown(group1, currentTime);
            callbacks.Received(0).ChildItemAccessFailed(Arg.Any<ReadRegisterGroup>(), Arg.Any<DateTime>());

        }

        [Fact]
        public void TestUpPriority()
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister, Name = "1" },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister, Name = "2" },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.HoldingRegister, Name = "3" },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(3), RegType = ConfigRegisterType.HoldingRegister, Name = "4" }
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegisterGroup>>();

            var group1 = new ReadRegisterGroup(settings1, callbacks, "");

            var currentTime = DateTime.Now;

            foreach(var r in group1.Registers) r.ValueReadedFromDevice(currentTime);

            callbacks.ClearReceivedCalls();

            // act
            group1.Registers.Single(r => r.StartNumber == 4).ValueWritedToDevice(currentTime.AddSeconds(1));
            var mostPriority = group1.EnsureMostPrioriyItem();

            // arrange
            mostPriority.StartNumber.Should().Be(4);

            callbacks.Received(1).ChildItemPriorityUp(Arg.Any<ReadRegisterGroup>(), Arg.Any<DateTime>());
            callbacks.Received(1).ChildItemPriorityUp(group1, currentTime.AddSeconds(1));
            callbacks.Received(0).ChildItemPriorityDown(Arg.Any<ReadRegisterGroup>(), Arg.Any<DateTime>());
            callbacks.Received(0).ChildItemAccessFailed(Arg.Any<ReadRegisterGroup>(), Arg.Any<DateTime>());
        }

        [Fact]
        public void TestReadTask()
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister, Name = "1" },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.HoldingRegister, Name = "2" },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(3), RegType = ConfigRegisterType.HoldingRegister, Name = "3" },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister, Name = "4" }
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegisterGroup>>();
            var group1 = new ReadRegisterGroup(settings1, callbacks, "");

            var currentTime = DateTime.Now;
            foreach (var r in group1.Registers) r.ValueReadedFromDevice(currentTime);
            callbacks.ClearReceivedCalls();

            var expectedReadTask = group1.Registers;

            // act
            var readTask = group1.GetReadTask(10, 10, currentTime.AddMinutes(1));

            // arrange
            readTask.Should().BeEquivalentTo(expectedReadTask);
        }

        [Fact]
        public void TestReadTask2()
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(500), RegType = ConfigRegisterType.HoldingRegister, Name = "1" },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister  , Name = "2" },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.HoldingRegister  , Name = "3" },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(500), RegType = ConfigRegisterType.HoldingRegister, Name = "4" }
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegisterGroup>>();
            var group1 = new ReadRegisterGroup(settings1, callbacks, "");

            var currentTime = DateTime.Now;
            foreach (var r in group1.Registers) r.ValueReadedFromDevice(currentTime);
            callbacks.ClearReceivedCalls();

            var expectedReadTask = group1.Registers.GetSegment(1, 2);

            // act
            var readTask = group1.GetReadTask(10, 10, currentTime.AddMinutes(1));

            // arrange
            readTask.Should().BeEquivalentTo(expectedReadTask);
        }

        [Fact]
        public void TestReadTaskSmallHole()
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister  , Name = "1" },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(500), RegType = ConfigRegisterType.HoldingRegister, Name = "2" },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(3), RegType = ConfigRegisterType.HoldingRegister  , Name = "3" },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister  , Name = "4" }
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegisterGroup>>();
            var group1 = new ReadRegisterGroup(settings1, callbacks, "");

            var currentTime = DateTime.Now;
            foreach (var r in group1.Registers) r.ValueReadedFromDevice(currentTime);
            callbacks.ClearReceivedCalls();

            var expectedReadTask = group1.Registers;

            // act
            var readTask = group1.GetReadTask(10, 10, currentTime.AddMinutes(1));

            // arrange
            readTask.Should().BeEquivalentTo(expectedReadTask);
        }

        [Fact]
        public void TestReadTaskBigHoleAfter()
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister  , Name = "1" },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.HoldingRegister  , Name = "2" },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(500), RegType = ConfigRegisterType.HoldingRegister, Name = "3" },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister  , Name = "4" }
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegisterGroup>>();
            var group1 = new ReadRegisterGroup(settings1, callbacks, "");

            var currentTime = DateTime.Now;
            foreach (var r in group1.Registers) r.ValueReadedFromDevice(currentTime);
            callbacks.ClearReceivedCalls();

            var expectedReadTask = group1.Registers.GetSegment(0, 2);

            // act
            var readTask = group1.GetReadTask(10, 0, currentTime.AddMinutes(1));

            // arrange
            readTask.Should().BeEquivalentTo(expectedReadTask);
        }

        [Fact]
        public void TestReadTaskBigHoleBefore()
        {
            // arrange
            var settings1 = new ModbusRegisterCompleted[]
            {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(10), RegType = ConfigRegisterType.HoldingRegister , Name = "1" },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(500), RegType = ConfigRegisterType.HoldingRegister, Name = "2" },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister  , Name = "3" },
                new() { Number = 4, ReadPeriod = TimeSpan.FromSeconds(10), RegType = ConfigRegisterType.HoldingRegister , Name = "4" }
            };

            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadRegisterGroup>>();
            var group1 = new ReadRegisterGroup(settings1, callbacks, "");

            var currentTime = DateTime.Now;
            foreach (var r in group1.Registers) r.ValueReadedFromDevice(currentTime);
            callbacks.ClearReceivedCalls();

            var expectedReadTask = group1.Registers.GetSegment(2, 2);

            // act
            var readTask = group1.GetReadTask(10, 0, currentTime.AddMinutes(1));

            // arrange
            readTask.Should().BeEquivalentTo(expectedReadTask);
        }


    }
}
