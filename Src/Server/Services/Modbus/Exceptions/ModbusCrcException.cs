namespace ModbusMqttPublisher.Server.Services.Modbus.Exceptions
{
    public class ModbusCrcException : Exception
    {
        public ModbusCrcException(string? message) : base(message)
        {
        }
    }
}
