namespace ModbusMqttPublisher.Server.Services.Modbus.New.Exceptions
{
    public class ModbusCrcException : Exception
    {
        public ModbusCrcException(string? message) : base(message)
        {
        }
    }
}
