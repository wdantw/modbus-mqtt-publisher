using ModbusMqttPublisher.Server.Services.Modbus.Enums;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public class ReadInputRegistersHandler : ReadWordRegisstersHandler
    {
        public ReadInputRegistersHandler(byte requestSlaveAddress, ushort requestStartRegister, ushort requestRegisterCount)
            : base(requestSlaveAddress: requestSlaveAddress, requestStartRegister: requestStartRegister, requestRegisterCount: requestRegisterCount)
        {
        }

        public override ModbusFunctionCode RequestFunctionCode => ModbusFunctionCode.ReadInputRegisters;
    }
}
