using ModbusMqttPublisher.Server.Services.Modbus.New.Enums;

namespace ModbusMqttPublisher.Server.Services.Modbus.New.Handlers
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
