using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Contracts.Configs.Enums;
using ModbusMqttPublisher.Server.Contracts.Settings;
using System.Collections.Frozen;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadDevice : ReadComparableGroup<ReadDevice, ReadRegisterGroup>
    {
        private readonly TimeSpan? _minSleepTimeout;
        private readonly TimeSpan _errorSleepTimeout;
        
        
        private readonly int _maxReadBit;
        private readonly int _maxReadRegisters;
        private readonly int _maxBitHole;
        private readonly int _maxRegHole;
        private readonly FrozenDictionary<string, ReadRegister> _allRegisters;


        private readonly ReadRegisterGroup[] _groups;
        private DateTime? _nextAllowedAccessTime;

        protected override ReadRegisterGroup[] Items => _groups;
        protected override ReadDevice This => this;
        public ReadRegisterGroup[] Groups => _groups;
        public string Name { get; }
        public byte SlaveAddress { get; }
        public int ReadRetryCount { get; }
        public TimeSpan ReadTimeout { get; }
        public TimeSpan WriteTimeout { get; }


        public override DateTime NextReadTime
        {
            get
            {
                var baseNextReadTime = base.NextReadTime;
                return _nextAllowedAccessTime.HasValue && _nextAllowedAccessTime.Value > baseNextReadTime ? _nextAllowedAccessTime.Value : baseNextReadTime;
            }
        }
        
        public ReadDevice(ModbusDeviceComplete devSettings, IReadPriorityCallbacks<ReadDevice> callbacks, string? portName, string serialName)
            : base (callbacks)
        {
            _maxReadBit = devSettings.MaxReadBit ?? DefaultSettings.MaxReadBit;
            if (_maxReadBit <= 0) throw new ArgumentException("MaxReadBit должен быть больше 0");

            _maxReadRegisters = devSettings.MaxReadRegisters ?? DefaultSettings.MaxReadRegisters;
            if (_maxReadRegisters <= 0) throw new ArgumentException("MaxReadRegisters должен быть больше 0");

            _maxBitHole = devSettings.MaxBitHole ?? DefaultSettings.MaxBitHole;
            if (_maxBitHole <= 0) throw new ArgumentException("MaxBitHole должен быть больше 0");

            _maxRegHole = devSettings.MaxRegHole ?? DefaultSettings.MaxRegHole;
            if (_maxRegHole <= 0) throw new ArgumentException("MaxRegHole должен быть больше 0");

            ReadRetryCount = devSettings.ReadRetryCount ?? DefaultSettings.DefaultReadRetryCount;

            SlaveAddress = devSettings.SlaveAddress.AssertNotNull();
            var defaultDeviceName = MqttPath.TopicPathDelimeter + MqttPath.CombineTopicPath(portName ?? serialName, $"Dev{SlaveAddress}");

            _minSleepTimeout = devSettings.MinSleepTimeout;

            _errorSleepTimeout = devSettings.ErrorSleepTimeout ?? DefaultSettings.DefaultErrorSleepTimeout;
            
            ReadTimeout = devSettings.ReadTimeout ?? DefaultSettings.DefaultPortTimeout;
            
            WriteTimeout = devSettings.WriteTimeout ?? DefaultSettings.DefaultPortTimeout;
            
            Name = string.IsNullOrWhiteSpace(devSettings.Name) ? defaultDeviceName : MqttPath.CombineTopicPath(portName, devSettings.Name);

            _groups = devSettings.Registers
                .GroupBy(s => s.RegType?.GetRegisterType())
                .Select(g => new ReadRegisterGroup(g, this, Name))
                .ToArray();

            _allRegisters = _groups
                .SelectMany(x => x.Registers)
                .ToFrozenDictionary(x => x.Name);
        }

        protected override void ChildItemPriorityDown(ReadRegisterGroup register, DateTime accessTime)
        {
            base.ChildItemPriorityDown(register, accessTime);
            _nextAllowedAccessTime = _minSleepTimeout.HasValue ? accessTime + _minSleepTimeout.Value : null;
        }

        protected override void ChildItemPriorityUp(ReadRegisterGroup changedItem, DateTime accessTime)
        {
            base.ChildItemPriorityUp(changedItem, accessTime);
            _nextAllowedAccessTime = _minSleepTimeout.HasValue ? accessTime + _minSleepTimeout.Value : null;
        }

        protected override void ChildItemAccessFailed(ReadRegisterGroup changedItem, DateTime accessTime)
        {
            base.ChildItemAccessFailed(changedItem, accessTime);
            _nextAllowedAccessTime = accessTime + _errorSleepTimeout;
        }

        public ReadTask? GetReadTask(DateTime currTime)
        {
            var hottestGroup = EnsureMostPrioriyItem();

            var maxRegisterCount = hottestGroup.RegisterType.IsBitReg() ? _maxReadBit : _maxReadRegisters;
            var maxHoleSize = hottestGroup.RegisterType.IsBitReg() ? _maxBitHole : _maxRegHole;

            var registers = hottestGroup.GetReadTask(maxRegisterCount, maxHoleSize, currTime);

            if (!registers.HasValue)
                return null;

            return new ReadTask(registers.Value, this);
        }

        public WriteTask? GetWriteTask(string topicName)
        {
            if (!_allRegisters.TryGetValue(topicName, out var register))
                return null;

            return new WriteTask(register, this);
        }
    }
}
