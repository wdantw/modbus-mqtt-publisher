using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Handlers;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

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

        private readonly ref struct RegistertWithIndex
        {
            private readonly ReadRegister[] _registers;

            public RegistertWithIndex(ReadRegister[] registers, int index)
            {
                _registers = registers;
                Index = index;
                Register = registers[index];
            }

            public ReadRegister Register { get; }

            public int Index { get; }

            public bool CanIncrement() => Index + 1 < _registers.Length;

            public RegistertWithIndex Increment() => new RegistertWithIndex(_registers, Index + 1);

            public static bool operator == (RegistertWithIndex r1, RegistertWithIndex r2) => r1.Index == r2.Index;

            public static bool operator != (RegistertWithIndex r1, RegistertWithIndex r2) => !(r1 == r2);

            public override int GetHashCode()
                => throw new InvalidOperationException();

            public override bool Equals([NotNullWhen(true)] object? obj)
                => throw new InvalidOperationException();
        }

        public ArraySegment<ReadRegister>? GetReadTask(int maxRegisterCount, int maxHoleSize, DateTime currTime)
        {
            var hottestRegister = EnsureMostPrioriyItem();

            // приоритетный регистр не нуждается в чтении
            if (!hottestRegister.NeedReadingNow(currTime))
                return null;

            var startRegister = new RegistertWithIndex(_registers, 0);
            while (!startRegister.Register.NeedReadingNow(currTime))
            {
                // Достигли конца массива. нет регистров готовых для чтения
                if (!startRegister.CanIncrement())
                    return null;

                startRegister = startRegister.Increment();
            }

            var currRegister = startRegister;
            
            var lastRegister = startRegister;
            bool hotInRange = startRegister.Register == hottestRegister;

            while (currRegister.CanIncrement())
            {
                currRegister = currRegister.Increment();

                // размер "дыры" включает регистры, которые не нуждаются в чтении
                var holeSize = currRegister.Register.StartNumber - lastRegister.Register.EndNumber;
                bool holeSizeExceeded = holeSize > maxHoleSize;

                // в полученном диапазоне есть необходимый регистр, добавление еще одного нарушит ограничение на размер "дыры"
                if (holeSizeExceeded && hotInRange)
                    break;

                // регистр не нужнадется в чтении или его время еще не настало, не учитываем в расчете
                if (!currRegister.Register.NeedReadingNow(currTime))
                    continue;

                if (holeSizeExceeded)
                {
                    // hotInRange == false, иначе вышли бы из цикла раньше
                    // размер "дыры" превышен, но необходимого регистра нет в диапазоне. поиск диапазона чтения заново
                    startRegister = currRegister;
                }
                else
                {
                    // пытаемся добавить текущий регистр в диапазон для чтения, учитывая все условия.
                    
                    // сдвигаем начало диапазона так, что бы диапазон уложился в ограничение maxRegisterCount
                    bool cancelMove = false;
                    bool startRegChanged = false;
                    while (currRegister.Register.EndNumber - startRegister.Register.StartNumber > maxRegisterCount)
                    {
                        // если текущий регистр менее приоритетный, чем первый, то останавливаем поиск
                        cancelMove = hotInRange && startRegister.Register.HasMorePriorityForRead(currRegister.Register);

                        if (cancelMove)
                            break;

                        startRegister = startRegister.Increment();
                        startRegChanged = true;
                    }

                    if (cancelMove)
                    {
                        // может быть ситуация, что следующий регистр является поддиапазоном от текущего и он возможно мог бы влезть в окно для чтения
                        // но тогда результатом будут не подряд идущие регистры.
                        break;
                    }

                    if (startRegChanged)
                    {
                        // исключем регистры, которые попали "за одно"
                        while (!startRegister.Register.NeedReadingNow(currTime))
                        {
                            if (startRegister == lastRegister)
                            {
                                startRegister = currRegister;
                                break;
                            }

                            startRegister = startRegister.Increment();
                        }
                    }
                }

                lastRegister = currRegister;
                hotInRange = hotInRange || currRegister.Register == hottestRegister;
            }

            return _registers.GetSegment(startRegister.Index, lastRegister.Index - startRegister.Index + 1);
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
                case RegisterType.Coil: return WBEventType.Colil;
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

                for (int regIndex = 0; regIndex < _registers.Length; regIndex++)
                {
                    var reg = _registers[regIndex];
                    
                    if (reg.StartNumber > config.StartRegister + config.EventPriorities.Length)
                        return;

                    reg.WbEventActualPriority = config.EventPriorities[reg.StartNumber - config.StartRegister];
                }

                return;
            }

            throw new InvalidOperationException("Не найдены регистры для применения настроек");
        }
    }
}
