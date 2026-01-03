using ModbusMqttPublisher.Server.Services.Modbus.Enums;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class ReadCoilsHandler : ReadBitRegistersHandler
    {
        public ReadCoilsHandler(byte requestSlaveAddress, ushort requestStartRegister, ushort requestRegisterCount)
            : base(requestSlaveAddress: requestSlaveAddress, requestStartRegister: requestStartRegister, requestRegisterCount: requestRegisterCount)
        {
        }

        public override ModbusFunctionCode RequestFunctionCode => ModbusFunctionCode.ReadCoils;
    }
}
