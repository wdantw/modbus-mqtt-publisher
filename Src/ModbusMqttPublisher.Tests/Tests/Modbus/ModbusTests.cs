using FluentAssertions;
using ModbusMqttPublisher.Server.Services.Modbus.New;
using ModbusMqttPublisher.Server.Services.Modbus.New.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.New.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.New.Handlers;
using System;
using System.Collections.Generic;
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
    }
}
