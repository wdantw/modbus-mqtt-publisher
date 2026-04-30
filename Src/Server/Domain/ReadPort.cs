using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Contracts.Settings;
using System.Collections.Frozen;
using System.IO.Ports;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadPort : ReadComparableGroupBase<ReadDevice>
    {
        private readonly ReadDevice[] _devices;
        private readonly IDictionary<byte, ReadDevice> _deviceDictionary;

        public int BaudRate { get; }

        public int DataBits { get; }

        public Parity Parity { get; }

        public StopBits StopBits { get; }

        public TimeSpan ErrorSleepTimeout { get; }

        protected override ReadDevice[] Items => _devices;

        public ReadDevice[] Devices => _devices;

        public TimeSpan MinSleepTimeout { get; }

        public int RetryCount { get; }

        public TimeSpan ReadTimeout { get; }

        public TimeSpan WriteTimeout { get; }

        public byte MinSlaveAddress => _devices[0].SlaveAddress;

        public byte MaxSlaveAddress => _devices[_devices.Length - 1].SlaveAddress;

        public string SerialName { get; }

        public bool AllowWbEvents { get; }

        public ReadPort(ModbusPortComplete portSettings)
        {
            SerialName = portSettings.SerialName.AssertNotEmpty();
            
            DataBits = portSettings.DataBits ?? DefaultSettings.DataBits;
            
            Parity = portSettings.Parity ?? DefaultSettings.Parity;
            
            StopBits = portSettings.StopBits ?? DefaultSettings.StopBits;

            _devices = portSettings.Devices
                .Select(d => new ReadDevice(d, this, portSettings.Name, SerialName))
                // используется для получение min max значений адресов
                .OrderBy(d => d.SlaveAddress)
                .ToArray();

            if (_devices.Length == 0)
                throw new Exception("Нельзя создать порт без устройств");

            _deviceDictionary = _devices.ToFrozenDictionary(d => d.SlaveAddress);

            BaudRate = portSettings.BaudRate ?? DefaultSettings.BaudRate;
            
            MinSleepTimeout = portSettings.MinSleepTimeout ?? DefaultSettings.MinSleepTimeout(BaudRate);
            
            ErrorSleepTimeout = portSettings.ErrorSleepTimeout ?? DefaultSettings.DefaultErrorSleepTimeout;

            RetryCount = portSettings.RetryCount ?? DefaultSettings.DefaultReadRetryCount;
            ReadTimeout = portSettings.ReadTimeout ?? DefaultSettings.DefaultPortTimeout;
            WriteTimeout = portSettings.WriteTimeout ?? DefaultSettings.DefaultPortTimeout;

            AllowWbEvents = portSettings.AllowWbEvents ?? true;
        }

        public ReadTask? GetNextReadTask(DateTime currTime)
            => EnsureMostPrioriyItem().GetReadTask(currTime);

        public ReadDevice? GetDeice(byte slaveAddress)
        {
            if (_deviceDictionary.TryGetValue(slaveAddress, out var device))
                return device;

            return null;
        }

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
