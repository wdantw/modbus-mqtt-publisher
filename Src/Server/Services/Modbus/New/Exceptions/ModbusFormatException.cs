namespace ModbusMqttPublisher.Server.Services.Modbus.New.Exceptions
{
    public class ModbusFormatException : Exception
    {
        public ModbusFormatException(string? message) : base(message)
        {
        }
    }
}
