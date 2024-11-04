using FluentAssertions;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Domain;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace ModbusMqttPublisher.Tests.Tests.Domain
{
    public class ReadDeviceTests
    {
        private static readonly ModbusDeviceComplete devSettings1 =  new ModbusDeviceComplete()
        {
            MinSleepTimeout = null,
            MaxRegHole = 1,
            MaxBitHole = 2,
            MaxReadRegisters = 3,
            MaxReadBit = 5,
            ErrorSleepTimeout = TimeSpan.FromMinutes(1),
            Registers = new List<ModbusRegisterCompleted>() {
                new() { Number = 1, ReadPeriod = TimeSpan.FromSeconds(4), RegType = ConfigRegisterType.HoldingRegister },
                new() { Number = 2, ReadPeriod = TimeSpan.FromSeconds(3), RegType = ConfigRegisterType.HoldingRegister },
                new() { Number = 3, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.HoldingRegister32 },
                new() { Number = 5, ReadPeriod = TimeSpan.FromSeconds(1), RegType = ConfigRegisterType.HoldingRegister32 },

                new() { Number = 7, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.Coil },
                new() { Number = 8, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.Coil },
                new() { Number = 9, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.Coil },
                new() { Number = 10, ReadPeriod = TimeSpan.FromSeconds(2), RegType = ConfigRegisterType.Coil },
            }
        };

        public void CreateSuccess()
        {
            // arragne
            var callbacks = Substitute.For<IReadPriorityCallbacks<ReadDevice>>();

            // act
            var dev = new ReadDevice(devSettings1, callbacks, null, "COM1");

            // assert
            dev.Groups.Length.Should().Be(2);
        }
    }
}
