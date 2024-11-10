using FluentAssertions;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Services.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ModbusMqttPublisher.Tests.Tests.Domain
{
    public class ValueTests
    {
        [Theory]
        [MemberData(nameof(FromModbusNumericTestData))]
        public void FromModbusNumericTest(RegisterFormat format, ushort[] data, string result)
        {
            // arrange
            var value = RegisterValueStorageFactory.Create(
                scale: null,
                precision: null,
                decimalSeparator: null,
                compareDiff: null,
                registerType: RegisterType.HoldingRegister,
                registerFormat: format
                );

            // act
            value.FromModbus(data);
            var strValue = MqttStringConverter.FromMqtt(value.ToMqtt());

            // assert
            strValue.Should().Be(result);
        }

        public static IEnumerable<object[]> FromModbusNumericTestData()
        {
            yield return new object[] { RegisterFormat.Default, new ushort[] { 0xFF85 }, "65413" };
            yield return new object[] { RegisterFormat.Int16, new ushort[] { 0xFF85 }, "-123" };
            yield return new object[] { RegisterFormat.Uint32, new ushort[] { 0xFF85, 0xFFF0 }, "4293984133" };
            yield return new object[] { RegisterFormat.Int32, new ushort[] { 0xFF85, 0xFFF0 }, "-983163" };
            yield return new object[] { RegisterFormat.Uint64, new ushort[] { 0xFF85, 0xFFF0, 0xFFFF, 0xFFF1 }, "18442803424034619269" };
            yield return new object[] { RegisterFormat.Int64, new ushort[] { 0xFF85, 0xFFF0, 0xFFFF, 0xFFF1 }, "-3940649674932347" };

            yield return new object[] { RegisterFormat.Uint32BE, new ushort[] { 0xFF85, 0xFFF0 }.Reverse().ToArray(), "4293984133" };
            yield return new object[] { RegisterFormat.Int32BE, new ushort[] { 0xFF85, 0xFFF0 }.Reverse().ToArray(), "-983163" };
            yield return new object[] { RegisterFormat.Uint64BE, new ushort[] { 0xFF85, 0xFFF0, 0xFFFF, 0xFFF1 }.Reverse().ToArray(), "18442803424034619269" };
            yield return new object[] { RegisterFormat.Int64BE, new ushort[] { 0xFF85, 0xFFF0, 0xFFFF, 0xFFF1 }.Reverse().ToArray(), "-3940649674932347" };
        }

        [Fact]
        public void FromModbusStringTest()
        {
            // arrange
            var value = RegisterValueStorageFactory.Create(
                scale: null,
                precision: null,
                decimalSeparator: null,
                compareDiff: null,
                registerType: RegisterType.HoldingRegister,
                registerFormat: RegisterFormat.String
                );

            // act
            value.FromModbus(new ushort[] { 0x3130, 0x3332, 0x4120, 0x0042, 0x0000 });
            var strValue = MqttStringConverter.FromMqtt(value.ToMqtt());

            // assert
            strValue.Should().Be("0123 AB");
        }
    }
}
