using ModbusMqttPublisher.Server.Services.Modbus.New.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.New.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.New.Handlers
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
            var result = new ushort[requestRegisterCount];

            for (var resultIndex = 0; resultIndex < requestRegisterCount; resultIndex++)
                result[resultIndex] = ByteOrderUtils.ToUInt16BE(dataBytes.Slice(resultIndex * 2, 2));

            return result;
        }
    }
}
