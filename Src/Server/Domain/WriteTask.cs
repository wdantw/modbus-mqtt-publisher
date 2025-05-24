namespace ModbusMqttPublisher.Server.Domain
{
    /// <summary>
    /// Задание записи в устройство.
    /// Регистр должен пренадлежать указанному устройству. это никак не контролируется
    /// </summary>
    public class WriteTask
    {
        public WriteTask(ReadRegister register, ReadDevice device)
        {
            Register = register;
            Device = device;
        }

        /// <summary>
        /// Реигстр устройства, в которое надо произвезти запись
        /// </summary>
        public ReadRegister Register { get; }

        /// <summary>
        /// Устройство, в которое надо произвести запись
        /// </summary>
        public ReadDevice Device { get; }
    }
}
