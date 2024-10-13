using ModbusMqttPublisher.Server.Contracts.Configs;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadRegister : IReadPriorityComparable<ReadRegister>
    {
        private readonly ushort _startNumber;
        private readonly TimeSpan? _readPeriod;
        private readonly Action<ReadRegister> _priorityUpCallback;
        private readonly Action<ReadRegister> _priorityDownCallback;

        // если null, значит никогда не читалось или было инвалидировано
        private DateTime? _lastReadTime = null;

        public ReadRegister(ModbusRegisterCompleted settings, Action<ReadRegister> priorityUpCallback, Action<ReadRegister> priorityDownCallback)
        {
            _startNumber = settings.Number ?? throw new ArgumentNullException("Не указан номер регистра");
            _readPeriod = settings.ReadPeriod;
            _priorityUpCallback = priorityUpCallback;
            _priorityDownCallback = priorityDownCallback;
        }

        // необходимо вызывать после записи значения в устройство
        public void ValueWritedToDevice(DateTime writeTime)
        {
            _lastReadTime = null;
            _priorityUpCallback(this);
        }

        // необходимо вызывать после чтения значения из устройства
        public void ValueReadedFromDevice(DateTime readTime)
        {
            _lastReadTime = readTime;
            _priorityDownCallback(this);
        }

        // нуждается ли регистр в чтении.
        private bool NeedReading()
            => !_lastReadTime.HasValue || _readPeriod.HasValue;

        public bool HasMorePriorityForRead(ReadRegister otherRegister)
        {
            if (!NeedReading())
                return false;

            if (!otherRegister.NeedReading())
                return true;

            // безусловный приоритет у регистров с невалидным значением
            if (_lastReadTime.HasValue != otherRegister._lastReadTime.HasValue)
                return !_lastReadTime.HasValue;

            if (!_lastReadTime.HasValue)
            {
                // оба регистра с невалидными значениями

                // переодически опрашиваемые регистры имеют приоритет
                if (_readPeriod.HasValue != otherRegister._readPeriod.HasValue)
                    return _readPeriod.HasValue;

                if (!_readPeriod.HasValue)
                {
                    // оба непереодических и с невалидными значениями. приоритет не определен
                    return true;
                }
                else
                {
                    // оба переоидическеие с невалидными значениями. приоритет у меньшего периода
                    return _readPeriod.Value < otherRegister._readPeriod!.Value;
                }
            }
            else
            {
                // оба регистра с валидными значениями
                // для обоих регистров _readPeriod.HasValue == true. иначе бы вышли в начале метода при проверке NeedReading()

                return _lastReadTime.Value + _readPeriod!.Value < otherRegister._lastReadTime!.Value + otherRegister._readPeriod!.Value;
            }
        }
    }
}
