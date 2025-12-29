using ModbusMqttPublisher.Server.Services.Modbus.Enums;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public ref struct ModbusHeader
    {
        public ModbusHeader(byte answerAddress, ModbusFunctionCode functionCode)
        {
            AnswerAddress = answerAddress;
            FunctionCode = functionCode;
        }

        public byte AnswerAddress { get; }

        public ModbusFunctionCode FunctionCode { get; }
    }
}
