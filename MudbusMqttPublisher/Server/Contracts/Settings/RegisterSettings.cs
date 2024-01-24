namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class RegisterSettings
    {
        public RegisterSettings(
            string name,
            ushort number,
            RegisterType regType,
            RegisterFormat regFormat,
            TimeSpan readPeriod,
            ushort? length,
            bool wbEvents,
            double? scale,
            int? precision)
        {
            Name = name;
            Number = number;
            RegType = regType;
            RegFormat = regFormat;
            ReadPeriod = readPeriod;
            Length = length;
            WbEvents = wbEvents;
            Scale = scale;
            Precision = precision;

            if (regFormat == RegisterFormat.String && !length.HasValue)
                throw new Exception("Не указана длина строки для строкового регистра");

            if (regFormat != RegisterFormat.String && length.HasValue)
                throw new Exception("Указана длина строки для не строкового регистра");

            if (SizeInRegisters > ushort.MaxValue - number)
                throw new Exception("Регистр выходит за гарицы адресного пространства");

            if (RegFormat == RegisterFormat.String && Scale.HasValue)
                throw new Exception("Параметр Scale неприменим к строкам");

            if (Precision.HasValue && !Scale.HasValue)
                throw new Exception("Параметр Precision применим только когда указан Scale");
        }

        // имя топика mqtt
        public string Name { get; private set; }

        // номер регистра
        public ushort Number { get; }

        // тип регистра
        public RegisterType RegType { get; }

        public RegisterFormat RegFormat { get; }

        public ushort? Length { get; }

        public TimeSpan ReadPeriod { get; }

        public bool WbEvents { get; }

        public double? Scale { get; }

        public int? Precision { get; }

        public int SizeInRegisters => (Length ?? 1) * RegFormat.SizeInRegisters();

        public ushort EndRegisterNumber => (ushort)(Number + (ushort)SizeInRegisters);
    }
}
