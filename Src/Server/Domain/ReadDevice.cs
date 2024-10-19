using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadDevice : ReadComparableGroup<ReadDevice, ReadRegisterGroup>
    {
        private readonly TimeSpan? _minSleepTimeout;
        private readonly TimeSpan _errorSleepTimeout;
        private readonly TimeSpan _readTimeout;
        private readonly TimeSpan _writeTimeout;

        private readonly ReadRegisterGroup[] _groups;
        private DateTime? _nextAllowedAccessTime;

        protected override ReadRegisterGroup[] Items => _groups;
        protected override ReadDevice This => this;
        public ReadRegisterGroup[] Groups => _groups;

        public override DateTime NextReadTime
        {
            get
            {
                var baseNextReadTime = base.NextReadTime;
                return _nextAllowedAccessTime.HasValue && _nextAllowedAccessTime.Value > baseNextReadTime ? _nextAllowedAccessTime.Value : baseNextReadTime;
            }
        }
        
        public ReadDevice(ModbusDeviceComplete devSettings, IReadPriorityCallbacks<ReadDevice> callbacks)
            : base (callbacks)
        {
            _minSleepTimeout = devSettings.MinSleepTimeout;
            _errorSleepTimeout = devSettings.ErrorSleepTimeout ?? DefaultSettings.DefaultErrorSleepTimeout;
            _readTimeout = devSettings.ReadTimeout ?? DefaultSettings.DefaultPortTimeout;
            _writeTimeout = devSettings.WriteTimeout ?? DefaultSettings.DefaultPortTimeout;

            _groups = devSettings.Registers
                .GroupBy(s => s.RegType?.GetRegisterType())
                .Select(g => new ReadRegisterGroup(g, this))
                .ToArray();
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
    }
}
