using ModbusMqttPublisher.Server.Services.Modbus.New.Enums;

namespace ModbusMqttPublisher.Server.Services.Modbus.New.Exceptions
{
    public class ModbusException : Exception
    {
        public ModbusException(byte address, ModbusFunctionCode functionCode, ModbusErrorCode errorCode)
            : base($"Устройство {address} вернуло ошибку {errorCode} для функции {functionCode}")
        {
            Address = address;
            FunctionCode = functionCode;
            ErrorCode = errorCode;
        }

        public byte Address { get; }

        public ModbusFunctionCode FunctionCode { get; }

        public ModbusErrorCode ErrorCode { get; }
    }
}
