using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Exceptions;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public abstract class ReadWordRegisstersHandler : ReadRegistersBaseHandler<ushort[]>
    {
        protected ReadWordRegisstersHandler(byte requestSlaveAddress, ushort requestStartRegister, ushort requestRegisterCount)
            : base(requestSlaveAddress: requestSlaveAddress, requestStartRegister: requestStartRegister, requestRegisterCount: requestRegisterCount)
        {
            if (requestRegisterCount > ModbusConstants.WordRegisterMaxReadPerRequest)
                throw new ArgumentException(nameof(requestRegisterCount));
        }

        protected override byte GetExpectedDataByteCount(ushort requestRegisterCount)
            => (byte)(requestRegisterCount * 2);

        protected override ushort[] GetResult(ReadOnlySpan<byte> dataBytes, ushort requestRegisterCount)
        {
            if (dataBytes.Length % 2 != 0)
                throw new ModbusFormatException("Размер данных должен быть кратен двум");

            var regCount = dataBytes.Length / 2;

            var result = new ushort[regCount];

            for (var resultIndex = 0; resultIndex < regCount; resultIndex++)
                result[resultIndex] = ByteOrderUtils.ToUInt16BE(dataBytes.Slice(resultIndex * 2, 2));

            return result;
        }
    }
}
