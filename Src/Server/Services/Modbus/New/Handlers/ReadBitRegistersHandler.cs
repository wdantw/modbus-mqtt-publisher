using ModbusMqttPublisher.Server.Services.Modbus.New.Enums;

namespace ModbusMqttPublisher.Server.Services.Modbus.New.Handlers
{
    public abstract class ReadBitRegistersHandler : ReadRegistersBaseHandler<bool[]>
    {
        protected ReadBitRegistersHandler(byte requestSlaveAddress, ushort requestStartRegister, ushort requestRegisterCount)
            : base(requestSlaveAddress: requestSlaveAddress, requestStartRegister: requestStartRegister, requestRegisterCount: requestRegisterCount)
        {
            if (requestRegisterCount > ModbusConstants.BitRegisterMaxReadPerRequest)
                throw new ArgumentException(nameof(requestRegisterCount));
        }

        protected override byte GetExpectedDataByteCount(ushort requestRegisterCount)
            => (byte)((requestRegisterCount + 7) / 8);

        protected override bool[] GetResult(ReadOnlySpan<byte> dataBytes, ushort requestRegisterCount)
        {
            var result = new bool[requestRegisterCount];

            for (var resultIndex = 0; resultIndex < requestRegisterCount; resultIndex++)
                result[resultIndex] = (dataBytes[resultIndex / 8] & 1 << resultIndex % 8) != 0;

            return result;
        }
    }
}
