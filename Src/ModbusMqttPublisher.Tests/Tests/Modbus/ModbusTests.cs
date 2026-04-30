using FluentAssertions;
using ModbusMqttPublisher.Server.Services.Modbus;
using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.Handlers;
using ModbusMqttPublisher.Server.Services.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ModbusMqttPublisher.Tests.Tests.Modbus
{
    public class ModbusTests
    {
        public class TestChannel : IModbusChannel
        {
            private readonly List<byte> _writed = new List<byte>();
            private readonly List<byte> _toRead = new List<byte>();
            private int _readPosition = 0;

            public List<byte> Writed => _writed;

            public List<byte> ToRead => _toRead;

            public void DiscardInBuffer()
            {
            }

            public void Read(byte[] buffer, int offset, int count)
            {
                _toRead.CopyTo(_readPosition, buffer, offset, count);
                _readPosition += count;
            }

            public void Write(byte[] buffer, int offset, int count)
                => _writed.AddRange(buffer.AsSpan().Slice(offset, count));
        }

        [Fact]
        public void TestFailedRequest()
        {
            // arrange
            var request = new byte[]
            {
                0x01, // device slave address
                0x01, // function: read coils
                0xFF, // start register hi
                0xFF, // start register lo
                0x00, // register count hi
                0x08, // register count lo
                0x3D, // crc hi
                0xE8, // crc lo
            };

            var response = new byte[]
            {
                0x01, // device slave address
                0x81, // function: error + read coils
                0x03, // error code
                0x00, // crc hi
                0x51, // crc lo
            };

            var channel = new TestChannel();
            channel.ToRead.AddRange(response);
            var modbus = new ModbusRtuProtocol(channel);

            // act
            var act = () => modbus.PerformRequest(new ReadCoilsHandler(requestSlaveAddress: 1, requestStartRegister: 0xFFFF, requestRegisterCount: 8));

            // asserts
            var exeption = act.Should().Throw<ModbusException>().And;
            exeption.Address.Should().Be(1);
            exeption.FunctionCode.Should().Be(ModbusFunctionCode.ReadCoils);
            exeption.ErrorCode.Should().Be(ModbusErrorCode.IllegalDataValue);
            channel.Writed.Should().BeEquivalentTo(request);
        }

        private static byte[] HexStringToBytes(string hexStr)
        {
            hexStr = hexStr.Replace(" ", "");
            return Enumerable.Range(0, hexStr.Length / 2).Select(x => Convert.ToByte(hexStr.Substring(x * 2, 2), 16)).ToArray(); ;
        }

        public static ushort[] ToBEArray(ulong value)
        {
            var result = new ushort[4];
            RegBitConverter.WriteDataBE(result, value);
            return result;
        }

        [Theory]
        [MemberData(nameof(SuccessTestData))]
        public void SuccessTest<TResult>(string requestStr, string responseStr, IModbusRequestHandler<TResult> handler, TResult expectedResult)
        {
            // arrange
            var request = HexStringToBytes(requestStr);
            var response = HexStringToBytes(responseStr);

            var channel = new TestChannel();
            channel.ToRead.AddRange(response);
            var modbus = new ModbusRtuProtocol(channel);

            // act
            var actualResult = modbus.PerformRequest(handler);

            // asserts
            request.Should().BeEquivalentTo(channel.Writed);
            actualResult.Should().BeEquivalentTo(expectedResult);
        }

        public static IEnumerable<object?[]> SuccessTestData()
        {
            // ReadDiscreteInputs

            yield return new object?[]
            {
                "01 02 00 14 00 01 F9 CE",
                "01 02 01 01 60 48",
                new ReadDescreteInputsHandler(1, 20, 1),
                new bool[] { true }
            };

            // ReadCoils

            yield return new object?[]
            {
                "01 01 00 00 00 02 BD CB",
                "01 01 01 02 D0 49",
                new ReadCoilsHandler(1, 0, 2),
                new bool[] { false, true }
            };

            // WriteSingleCoil

            yield return new object?[]
            {
                "01 05 00 00 FF 00 8C 3A",
                "01 05 00 00 FF 00 8C 3A",
                new WriteSingleCoilHandler(1, 0, true),
                NoResult.Value
            };

            // WriteMultipleCoils

            yield return new object?[]
            {
                "7B 0F 30 39 00 09 02 FE 01 4A D4",
                "7B 0F 30 39 00 09 41 5A",
                new WriteMultipleCoilsHandler(123, 12345, new bool[] { false, true, true, true, true, true, true, true, true }),
                NoResult.Value
            };

            // ReadInputRegisters

            yield return new object?[]
            {
                "01 04 00 28 00 01 B1 C2",
                "01 04 02 00 66 39 1A",
                new ReadInputRegistersHandler(1, 40, 1),
                new ushort[] { 0x66 }
            };

            // ReadHoldingRegisters

            yield return new object?[]
            {
                "01 03 00 6E 00 02 A5 D6",
                "01 03 02 00 15 79 8B",
                new ReadHoldingRegistersHandler(1, 110, 2),
                new ushort[] { 0x0015 }
            };

            yield return new object?[]
            {
                "01 03 00 46 00 01 65 DF",
                "01 03 02 00 15 79 8B",
                new ReadHoldingRegistersHandler(1, 70, 1),
                new ushort[] { 0x15 }
            };


            yield return new object?[]
            {
                "01 03 00 1E 00 04 24 0F",
                "01 03 08 01 02 03 04 05 06 07 08 65 13",
                new ReadHoldingRegistersHandler(1, 30, 4),
                new ushort[] { 0x0102, 0x0304, 0x0506, 0x0708 }
            };

            // WriteSingleRegister

            yield return new object?[]
            {
                "01 06 00 5E 0F 41 2D D8",
                "01 06 00 5E 0F 41 2D D8",
                new WriteSingleRegisterHandler(1, 94, 0x0F41),
                NoResult.Value
            };

            // WriteMultipleRegisters

            yield return new object?[]
            {
                "01 10 00 63 00 01 02 01 23 EF 8A",
                "01 10 00 63 00 01 F1 D7",
                new WriteMultipleRegistersHandler(1, 99, new ushort[] { 0x0123 }),
                NoResult.Value
            };

            yield return new object?[]
            {
                "01 10 00 5F 00 04 08 01 23 45 67 89 AB CD EF C4 5D",
                "01 10 00 5F 00 04 F1 D8",
                new WriteMultipleRegistersHandler(1, 95, ToBEArray(0x0123456789ABCDEFUL)),
                NoResult.Value
            };

            // WbRequestEventsHandler

            yield return new object?[]
            {
                "FD 46 10 00 F8 00 00 79 5B",
                "FF FF FF FF FF FF FD 46 12 52 5D",
                new WbRequestEventsHandler(),
                null
            };

            yield return new object?[]
            {
                "FD 46 10 00 F8 00 00 79 5B",
                "FF FF FF FF FF FF 01 46 11 00 01 04 00 0F 00 00 3B 73",
                new WbRequestEventsHandler(),
                new WbEvents(1, 0, 1, new WbEvent[] { new WbEvent(WBEventType.System, (ushort)WbSystemEventId.Rebooted, null) }),
            };

            // WbConfigureEventsHandler

            var cfg = new WbEventConfig[]
            {
                new WbEventConfig(WBEventType.Holding, 1, new WbEventPriority[] { WbEventPriority.Low }),
                new WbEventConfig(WBEventType.System, (ushort)WbSystemEventId.Rebooted, new WbEventPriority[] { WbEventPriority.Disabled })
            };

            yield return new object?[]
            {
                "01 46 18 0A 03 00 01 01 01 0F 00 00 01 00 28 7D",
                "01 46 18 02 01 00 2E F5",
                new WbConfigureEventsHandler(1, cfg),
                cfg,
            };
        }
    }
}
