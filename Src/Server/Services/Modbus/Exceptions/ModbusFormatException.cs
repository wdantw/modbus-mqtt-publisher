namespace ModbusMqttPublisher.Server.Services.Modbus.Exceptions
{
    public class ModbusFormatException : Exception
    {
        public ModbusFormatException(string? message) : base(message)
        {
        }
    }
}
