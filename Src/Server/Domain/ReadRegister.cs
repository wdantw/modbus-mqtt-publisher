using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Configs;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadRegister : IReadPriorityComparable
    {
        private readonly ushort _startNumber;
        private readonly ushort _endNumber;
        private readonly TimeSpan? _readPeriod;
        private readonly IReadPriorityCallbacks<ReadRegister> _callbacks;

        // если Null, то чтения не требуется
        private DateTime _nextReadTime;

        public ushort StartNumber => _startNumber;
        public ushort EndNumber => _endNumber;
        public DateTime NextReadTime => _nextReadTime;

        public ReadRegister(ModbusRegisterCompleted settings, IReadPriorityCallbacks<ReadRegister> callbacks)
        {
            _startNumber = settings.Number ?? throw new ArgumentException("Не указан номер регистра");
            _endNumber = (ushort)(_startNumber + (settings.RegType!.Value.GetRegisterFormat().SizeInRegisters() * (settings.Length ?? 1)));
            _readPeriod = settings.ReadPeriod;
            _callbacks = callbacks;

            _nextReadTime = DateTime.MinValue + (_readPeriod ?? TimeSpan.FromDays(365));
        }

        private void SetNextReadTime(DateTime nextReadTime, DateTime accessTime)
        {
            var compareRes = nextReadTime.CompareTo(_nextReadTime);
            _nextReadTime = nextReadTime;

            if (compareRes < 0)
                _callbacks.ChildItemPriorityUp(this, accessTime);

            if (compareRes > 0)
                _callbacks.ChildItemPriorityDown(this, accessTime);
        }

        // необходимо вызывать после записи значения в устройство
        public void ValueWritedToDevice(DateTime writeTime)
        {
            SetNextReadTime(DateTime.MinValue, writeTime);
        }

        // необходимо вызывать после чтения значения из устройства
        public void ValueReadedFromDevice(DateTime readTime)
        {
            SetNextReadTime(_readPeriod.HasValue ? readTime + _readPeriod.Value : DateTime.MaxValue, readTime);
        }

        public void AccessFailed(DateTime accessTime)
        {
            _callbacks.ChildItemAccessFailed(this, accessTime);
        }
    }
}
