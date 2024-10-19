using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts.Configs;
using System.Diagnostics.CodeAnalysis;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadRegisterGroup : ReadComparableGroup<ReadRegisterGroup, ReadRegister>
    {
        private readonly ReadRegister[] _registers;
        protected override ReadRegister[] Items => _registers;
        protected override ReadRegisterGroup This => this;
        public ReadRegister[] Registers => _registers;

        public ReadRegisterGroup(IEnumerable<ModbusRegisterCompleted> regSettings, IReadPriorityCallbacks<ReadRegisterGroup> callbacks)
            : base(callbacks)
        {
            _registers = regSettings
                .Select(set =>
                    new ReadRegister(
                        settings: set,
                        this
                        )
                )
                .OrderBy(r => r.StartNumber)
                .ThenBy(r => r.EndNumber)
                .ToArray();

            if (_registers.Length == 0)
                throw new ApplicationException("Группа должна содержать хотя бы один регистр");
        }

        private struct RegistertWithIndex
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

            public static bool operator ==(RegistertWithIndex r1, RegistertWithIndex r2) => r1.Index == r2.Index;
            public static bool operator !=(RegistertWithIndex r1, RegistertWithIndex r2) => !(r1 == r2);

            public override bool Equals([NotNullWhen(true)] object? obj)
                => (obj is RegistertWithIndex) && ((RegistertWithIndex)obj) == this;

            public override int GetHashCode() => Index.GetHashCode();

            public override string ToString()
                => $"Reg {Index} (Number: {Register.StartNumber})";
        }

        public ReadTask? GetReadTask(
            int maxRegisterCount,
            int maxHoleSize,
            DateTime currTime
            )
        {
            var hottestRegister = EnsureMostPrioriyItem();
            
            var startRegister = new RegistertWithIndex(_registers, 0);
            while (!startRegister.Register.NeedReadingNow(currTime))
            {
                if (startRegister.Register == hottestRegister)
                {
                    // приоритетный регистр не нуждается в чтении
                    return null;
                }

                if (!startRegister.CanIncrement())
                {
                    // Достигли конца массива. нет регистров готовых для чтения
                    return null;
                }
                startRegister = startRegister.Increment();
            }

            var lastRegister = startRegister;
            bool hotInRange = startRegister.Register == hottestRegister;

            var currRegister = startRegister;
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

            return new ReadTask(_registers.GetSegment(startRegister.Index, lastRegister.Index - startRegister.Index + 1));
        }
    }
}
