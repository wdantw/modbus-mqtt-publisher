namespace ModbusMqttPublisher.Server.Domain
{
    public class WriteTask
    {
        public WriteTask(ReadRegister register, ReadDevice device)
        {
            Register = register;
            Device = device;
        }

        public ReadRegister Register { get; }
        public ReadDevice Device { get; }
    }
}
