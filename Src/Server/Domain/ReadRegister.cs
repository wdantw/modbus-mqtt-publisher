using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Contracts.Configs.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Values;
using MQTTnet.DependencyInjection;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadRegister : IReadPriorityComparable
    {
        private readonly TimeSpan? _readPeriod;
        private readonly bool _forcePublishOnRead;
        private readonly IReadPriorityCallbacks<ReadRegister> _callbacks;
        private readonly IRegisterValueStorageWithInConverter _registerValue;
        private bool _registerValueIsActual = false;

        public ushort StartNumber { get; }
        
        public ushort EndNumber { get; }
        
        public byte SizeInRegisters { get; }

        public string Name { get; }
        
        public RegisterType RegisterType { get; }
        
        public IIncomeRegisterConverter IncomeValueConverter => _registerValue;
        
        public IPublishValueSorage PublishValue => _registerValue;

        public DateTime NextReadTime { get; private set; }

        public bool WWbEventsSupport { get; }

        public WbEventPriority WbEventRequestedPriority { get; }
        
        public WbEventPriority WbEventActualPriority { get; set; } = WbEventPriority.Disabled;

        public ReadRegister(ModbusRegisterCompleted settings, IReadPriorityCallbacks<ReadRegister> callbacks, string baseName)
        {
            StartNumber = settings.Number.AssertNotNull();
            var regType = settings.RegType.AssertNotNull().Validate();
            RegisterType = regType.GetRegisterType().Validate();
            _forcePublishOnRead = settings.ForcePublish ?? false;
            _readPeriod = settings.ReadPeriod;
            _callbacks = callbacks;
            Name = MqttPath.CombineTopicPath(baseName, settings.Name.AssertNotEmpty());
            var registerFormat = regType.GetRegisterFormat().Validate();

            SizeInRegisters = regType.SizeInRegisters(settings.Length);
            if (SizeInRegisters > ushort.MaxValue - StartNumber)
                throw new Exception("Регистр выходит за гарицы адресного пространства");
            EndNumber = (ushort)(StartNumber + SizeInRegisters);

            NextReadTime = DateTime.MinValue + (_readPeriod ?? TimeSpan.FromDays(365));

            WWbEventsSupport = settings.WbEvents ?? false;
            WbEventRequestedPriority = settings.WbEventPriority ?? WbEventPriority.Low;

            _registerValue = RegisterValueStorageFactory.Create(
                settings.Scale,
                settings.Precision,
                settings.DecimalSeparator,
                settings.CompareDiff,
                RegisterType,
                registerFormat);
        }

        private void SetNextReadTime(DateTime nextReadTime, DateTime accessTime)
        {
            if (WbEventActualPriority != WbEventPriority.Disabled)
                nextReadTime = DateTime.MaxValue;

            var compareRes = nextReadTime.CompareTo(NextReadTime);
            NextReadTime = nextReadTime;

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

        public bool ReadFromModbus(DateTime readTime, ReadOnlySpan<bool> data)
        {
            ValueReadedFromDevice(readTime);

            var res = _registerValue.FromModbus(data) || !_registerValueIsActual;
            _registerValueIsActual = true;
            return res || _forcePublishOnRead;
        }

        public bool ReadFromModbus(DateTime readTime, ReadOnlySpan<ushort> data)
        {
            ValueReadedFromDevice(readTime);

            var res = _registerValue.FromModbus(data) || !_registerValueIsActual;
            _registerValueIsActual = true;
            return res || _forcePublishOnRead;
        }

        public void DeviceRebooted()
        {
            NextReadTime = DateTime.MinValue;
            WbEventActualPriority = WbEventPriority.Disabled;
        }
    }
}
