namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class RegisterSettings
    {
        public RegisterSettings(RegisterSettings source, string namePrefix)
            : this(source)
        {
            Name = namePrefix + Name;
        }

        public RegisterSettings(RegisterSettings source)
        {
            Name = source.Name;
            Number = source.Number;
            RegType = source.RegType;
            RegFormat = source.RegFormat;
            ReadPeriod = source.ReadPeriod;
            Length = source.Length;
            WbEvents = source.WbEvents;
            Scale = source.Scale;
        }

        public RegisterSettings(string name, ushort number, RegisterType regType, RegisterFormat regFormat, TimeSpan readPeriod, ushort? length, bool wbEvents, double? scale)
        {
            Name = name;
            Number = number;
            RegType = regType;
            RegFormat = regFormat;
            ReadPeriod = readPeriod;
            Length = length;
            WbEvents = wbEvents;
            Scale = scale;

            if (regFormat == RegisterFormat.String && !length.HasValue)
                throw new Exception("Не указана длина строки для строкового регистра");

            if (regFormat != RegisterFormat.String && length.HasValue)
                throw new Exception("Указана длина строки для не строкового регистра");

            if (SizeInRegisters > ushort.MaxValue - number)
                throw new Exception("Регистр выходит за гарицы адресного пространства");

            if (RegFormat == RegisterFormat.String && Scale.HasValue)
                throw new Exception("Параметр Scale неприменим к строкам");
        }

        // имя топика mqtt
        public string Name { get; private set; }

        // номер регистра
        public ushort Number { get; }

        // тип регистра
        public RegisterType RegType { get; }

        public RegisterFormat RegFormat { get; }

        public ushort? Length { get; }

        public TimeSpan ReadPeriod { get; private set; }

        public bool WbEvents { get; private set; }

        public double? Scale { get; private set; }

        public int SizeInRegisters => (Length ?? 1) * RegFormat.SizeInRegisters();

        public ushort EndRegisterNumber => (ushort)(Number + (ushort)SizeInRegisters);

        public void UpdateScale(double scale)
        {
            if (RegFormat == RegisterFormat.String)
                throw new Exception("Параметр Scale неприменим к строкам");

            Scale = scale;
        }

        public void UpdateWbEvents(bool wbEvents)
        {
            WbEvents = !wbEvents;
        }

        public void UpdatePeriod(TimeSpan readPeriod)
        {
            ReadPeriod = readPeriod;
        }
    }
}
