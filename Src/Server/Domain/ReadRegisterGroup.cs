using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Domain.FindRegRange;
using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Handlers;
using System.Collections.Frozen;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadRegisterGroup : ReadComparableGroup<ReadRegisterGroup, ReadRegister>
    {
        private readonly ReadRegister[] _registers;
        private readonly IDictionary<ushort, ReadRegister> _registersByAddress;

        protected override ReadRegister[] Items => _registers;
        
        protected override ReadRegisterGroup This => this;
        
        public ReadRegister[] Registers => _registers;
        
        public RegisterType RegisterType { get; }

        public ReadRegisterGroup(IEnumerable<ModbusRegisterCompleted> regSettings, IReadPriorityCallbacks<ReadRegisterGroup> callbacks, string baseName)
            : base(callbacks)
        {
            _registers = regSettings
                .Select(set =>
                    new ReadRegister(
                        settings: set,
                        this,
                        baseName
                        )
                )
                .OrderBy(r => r.StartNumber)
                .ThenBy(r => r.EndNumber)
                .ToArray();

            if (_registers.Length == 0)
                throw new ApplicationException("Группа должна содержать хотя бы один регистр");

            RegisterType = _registers.Select(r => r.RegisterType).Distinct().Single();
            _registersByAddress = _registers.ToFrozenDictionary(r => r.StartNumber);
        }

        private sealed class FindRegRangeAlgorithmRegisters : IFindRegRangeAlgorithmRegisters
        {
            private readonly ReadRegister[] _registers;
            private readonly DateTime _currTime;
            private readonly int _mostPriorityItemIndex;

            public FindRegRangeAlgorithmRegisters(ReadRegister[] registers, DateTime currTime, int mostPriorityItemIndex)
            {
                _registers = registers;
                _currTime = currTime;
                _mostPriorityItemIndex = mostPriorityItemIndex;
            }

            public int Count => _registers.Length;

            public int GetMostPriorityItemIndex() => _mostPriorityItemIndex;

            public bool NeedReadingNow(int index) => _registers[index].NeedReadingNow(_currTime);

            public ushort StartAddress(int index) => _registers[index].StartNumber;

            public ushort EndAddress(int index) => _registers[index].EndNumber;

            public bool HasMoreOrEqualsPriority(int index1, int index2) => _registers[index1].HasMoreOrEqualsPriorityForRead(_registers[index2]);
        }

        public ArraySegment<ReadRegister>? GetReadTask(int maxRegisterCount, int maxHoleSize, DateTime currTime)
        {
            var hottestIndex = 0;
            var hottestRegister = EnsureMostPrioriyItem();
            while (_registers[hottestIndex] != hottestRegister)
                hottestIndex++;

            var result = FindRegRangeAlgorithm.Find(maxRegisterCount, maxHoleSize, new FindRegRangeAlgorithmRegisters(_registers, currTime, hottestIndex));

            if (result == null)
                return null;

            return _registers.GetSegment(result.Value.StartIndex, result.Value.Length);
        }
    
        public ReadRegister? GetRegisterByAddress(ushort address)
        {
            if (!_registersByAddress.TryGetValue(address, out var result))
                return null;

            return result;
        }
    
        private WBEventType GetWbEventType()
        {
            switch (RegisterType)
            {
                case RegisterType.Coil: return WBEventType.Coil;
                case RegisterType.DiscreteInput: return WBEventType.Discrete;
                case RegisterType.HoldingRegister: return WBEventType.Holding;
                case RegisterType.InputRegister: return WBEventType.Input;
                default:
                    throw new InvalidOperationException();
            }
        }

        public IEnumerable<WbEventConfig> GetWbEventConfigurations()
        {
            ushort? startRegConfigNumber = null;
            var priorities = new List<WbEventPriority>();

            foreach (var reg in _registers)
            {
                if (!reg.WWbEventsSupport)
                    continue;

                if (startRegConfigNumber.HasValue && startRegConfigNumber.Value + priorities.Count != reg.StartNumber)
                {
                    yield return new WbEventConfig(GetWbEventType(), startRegConfigNumber.Value, priorities.ToArray());
                    priorities.Clear();
                    startRegConfigNumber = null;
                }

                if (!startRegConfigNumber.HasValue)
                    startRegConfigNumber = reg.StartNumber;

                priorities.Add(reg.WbEventRequestedPriority);
            }

            if (startRegConfigNumber.HasValue)
                yield return new WbEventConfig(GetWbEventType(), startRegConfigNumber.Value, priorities.ToArray());
        }

        public void ApplyWbEventsConfiguration(WbEventConfig config)
        {
            for (int startRegIndex = 0; startRegIndex < _registers.Length; startRegIndex++)
            {
                var startReg = _registers[startRegIndex];

                if (startReg.StartNumber != config.StartRegister)
                    continue;

                for (int regIndex = startRegIndex; regIndex < _registers.Length; regIndex++)
                {
                    var reg = _registers[regIndex];
                    
                    if (reg.StartNumber >= config.StartRegister + config.EventPriorities.Length)
                        return;

                    reg.WbEventActualPriority = config.EventPriorities[reg.StartNumber - config.StartRegister];
                }

                return;
            }

            throw new InvalidOperationException("Не найдены регистры для применения настроек");
        }

        public void DeviceRebooted()
        {
            foreach (var reg in _registers)
                reg.DeviceRebooted();
        }
    }
}
