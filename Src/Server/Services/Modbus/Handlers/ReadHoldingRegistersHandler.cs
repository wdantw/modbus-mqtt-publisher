using ModbusMqttPublisher.Server.Services.Modbus.Enums;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class ReadHoldingRegistersHandler : ReadWordRegisstersHandler
    {
        public ReadHoldingRegistersHandler(byte requestSlaveAddress, ushort requestStartRegister, ushort requestRegisterCount)
            : base(requestSlaveAddress: requestSlaveAddress, requestStartRegister: requestStartRegister, requestRegisterCount: requestRegisterCount)
        {
        }

        public override ModbusFunctionCode RequestFunctionCode => ModbusFunctionCode.ReadHoldingRegisters;
    }
}
