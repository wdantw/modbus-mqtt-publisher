using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Contracts.Settings;
using System.IO.Ports;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadPort : ReadComparableGroupBase<ReadDevice>
    {
        public int BaudRate { get; }
        public int DataBits { get; }
        public Parity Parity { get; }
        public StopBits StopBits { get; }


        public TimeSpan ErrorSleepTimeout { get; }
        private readonly TimeSpan _readTimeout;
        private readonly TimeSpan _writeTimeout;
        private readonly ReadDevice[] _devices;

        private DateTime? _nextAllowedAccessTime;

        protected override ReadDevice[] Items => _devices;
        public ReadDevice[] Devices => _devices;

        public TimeSpan MinSleepTimeout { get; }

        public override DateTime NextReadTime
        {
            get
            {
                var baseNextReadTime = base.NextReadTime;
                return _nextAllowedAccessTime.HasValue && _nextAllowedAccessTime.Value > baseNextReadTime ? _nextAllowedAccessTime.Value : baseNextReadTime;
            }
        }

        public string SerialName { get; }

        public ReadPort(ModbusPortComplete portSettings)
        {
            SerialName = portSettings.SerialName.AssertNotEmpty();
            DataBits = portSettings.DataBits ?? DefaultSettings.DataBits;
            Parity = portSettings.Parity ?? DefaultSettings.Parity;
            StopBits = portSettings.StopBits ?? DefaultSettings.StopBits;

            _devices = portSettings.Devices
                .Select(d => new ReadDevice(d, this, portSettings.Name, SerialName)
                ).ToArray();

            BaudRate = portSettings.BaudRate ?? DefaultSettings.BaudRate;
            MinSleepTimeout = portSettings.MinSleepTimeout ?? DefaultSettings.MinSleepTimeout(BaudRate);
            ErrorSleepTimeout = portSettings.ErrorSleepTimeout ?? DefaultSettings.DefaultErrorSleepTimeout;
            _readTimeout = portSettings.ReadTimeout ?? DefaultSettings.DefaultPortTimeout;
            _writeTimeout = portSettings.WriteTimeout ?? DefaultSettings.DefaultPortTimeout;
        }

        protected override void ChildItemPriorityDown(ReadDevice register, DateTime accessTime)
        {
            base.ChildItemPriorityDown(register, accessTime);
            _nextAllowedAccessTime = accessTime + MinSleepTimeout;
        }

        protected override void ChildItemPriorityUp(ReadDevice changedItem, DateTime accessTime)
        {
            base.ChildItemPriorityUp(changedItem, accessTime);
            _nextAllowedAccessTime = accessTime + MinSleepTimeout;
        }

        protected override void ChildItemAccessFailed(ReadDevice changedItem, DateTime accessTime)
        {
            base.ChildItemAccessFailed(changedItem, accessTime);
            _nextAllowedAccessTime = accessTime + ErrorSleepTimeout;
        }

        public ReadTask? GetNextReadTask(DateTime currTime)
            => EnsureMostPrioriyItem().GetReadTask(currTime);

        public WriteTask? GetWriteTask(string topicName)
        {
            foreach(var dev in _devices)
            {
                var task = dev.GetWriteTask(topicName);
                if (task != null)
                    return task;
            }
            return null;
        }
    }
}
