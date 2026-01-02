using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Contracts.Configs.Enums;
using ModbusMqttPublisher.Server.Contracts.Settings;
using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Handlers;
using System.Collections.Frozen;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadDevice : ReadComparableGroup<ReadDevice, ReadRegisterGroup>
    {
        private readonly int _maxReadBit;
        private readonly int _maxReadRegisters;
        private readonly int _maxBitHole;
        private readonly int _maxRegHole;
        private readonly FrozenDictionary<string, ReadRegister> _allRegistersByTopic;
        private readonly FrozenDictionary<RegisterType, ReadRegisterGroup> _groupsByType;

        private readonly ReadRegisterGroup[] _groups;

        protected override ReadRegisterGroup[] Items => _groups;
        
        protected override ReadDevice This => this;
        
        public ReadRegisterGroup[] Groups => _groups;
        
        public string Name { get; }
        
        public byte SlaveAddress { get; }
        
        public bool NeedWbEventsConfigure { get; private set; } = true;

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

            SlaveAddress = devSettings.SlaveAddress.AssertNotNull();
            var defaultDeviceName = MqttPath.TopicPathDelimeter + MqttPath.CombineTopicPath(portName ?? serialName, $"Dev{SlaveAddress}");

            Name = string.IsNullOrWhiteSpace(devSettings.Name) ? defaultDeviceName : MqttPath.CombineTopicPath(portName, devSettings.Name);

            _groups = devSettings.Registers
                .GroupBy(s => s.RegType?.GetRegisterType())
                .Select(g => new ReadRegisterGroup(g, this, Name))
                .ToArray();

            _allRegistersByTopic = _groups
                .SelectMany(x => x.Registers)
                .ToFrozenDictionary(x => x.Name);

            _groupsByType = _groups.ToFrozenDictionary(g => g.RegisterType);
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
            if (!_allRegistersByTopic.TryGetValue(topicName, out var register))
                return null;

            return new WriteTask(register, this);
        }

        public void DeviceRebooted()
        {
            NeedWbEventsConfigure = true;
        }

        public ReadRegister? GetRegisterByAddress(RegisterType type, ushort address)
        {
            if (!_groupsByType.TryGetValue(type, out var group))
                return null;

            return group.GetRegisterByAddress(address);
        }

        public IEnumerable<WbEventConfig> GetWbEventConfigurations()
        {
            foreach(var group in _groups)
                foreach(var c in group.GetWbEventConfigurations())
                    yield return c;

            yield return new WbEventConfig(WBEventType.System, (ushort)WbSystemEventId.Rebooted, new WbEventPriority[] { WbEventPriority.Low });
        }

        public void ApplyWbEventsConfiguration(WbEventConfig[] configurations)
        {
            foreach(var cfg in configurations)
            {
                switch (cfg.EventType)
                {
                    case WBEventType.Colil:
                        _groupsByType[RegisterType.Coil].ApplyWbEventsConfiguration(cfg);
                        break;
                    case WBEventType.Discrete:
                        _groupsByType[RegisterType.DiscreteInput].ApplyWbEventsConfiguration(cfg);
                        break;
                    case WBEventType.Holding:
                        _groupsByType[RegisterType.HoldingRegister].ApplyWbEventsConfiguration(cfg);
                        break;
                    case WBEventType.Input:
                        _groupsByType[RegisterType.InputRegister].ApplyWbEventsConfiguration(cfg);
                        break;
                    case WBEventType.System:
                        // todo прочекать что лишнего нет
                        break;
                }
            }

            NeedWbEventsConfigure = false;
        }
    }
}
