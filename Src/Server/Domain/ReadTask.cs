using ModbusMqttPublisher.Server.Contracts;

namespace ModbusMqttPublisher.Server.Domain
{
    /// <summary>
    /// Задание по чтению из устройства.
    /// Регистры должен пренадлежать указанному устройству. это никак не контролируется.
    /// Так же регистры должы быть отсортированны в порядке возрастания номеров.
    /// Регистры должны иметь одинаковый тип
    /// </summary>
    public class ReadTask
    {
        public ReadTask(ArraySegment<ReadRegister> registers, ReadDevice device)
        {
            Registers = registers;
            Device = device;
        }

        /// <summary>
        /// Номер регистра с которого надо начать чтение из устройства
        /// </summary>
        public ushort StartNumber => Registers[0].StartNumber;

        /// <summary>
        /// Номер регистра, до которого читать (исключающий. т.е. указанный номер уже читать не надо)
        /// </summary>
        public ushort EndNumber => Registers[^1].EndNumber;

        /// <summary>
        /// Количество регистров для чтения
        /// </summary>
        public ushort RegisterCount => (ushort)(EndNumber - StartNumber);

        /// <summary>
        /// Тип регистров
        /// </summary>
        public RegisterType RegisterType => Registers[0].RegisterType;

        /// <summary>
        /// Устройство, к которому принадлежит регистр
        /// </summary>
        public ReadDevice Device { get; }
        
        /// <summary>
        /// Список регистров
        /// </summary>
        public ArraySegment<ReadRegister> Registers { get; }

        /// <summary>
        /// Уведомить регистры об ошибке чтения
        /// </summary>
        /// <param name="accessTime"></param>
        public void AccessFailed(DateTime accessTime)
        {
            foreach (var r in Registers)
                r.AccessFailed(accessTime);
        }
    }
}
