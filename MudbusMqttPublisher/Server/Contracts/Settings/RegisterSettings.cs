using MudbusMqttPublisher.Server.Services.Queues;

namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class RegisterSettings : IReadQueueRegister
	{
		public RegisterSettings(
			string name,
			ushort number,
			RegisterType regType,
			RegisterFormat regFormat,
			TimeSpan? readPeriod,
			ushort? length,
			bool wbEvents,
			double? scale,
			int? precision,
			string? decimalSeparator,
			double? compareDiff)
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
			DecimalSeparator = decimalSeparator;
			CompareDiff = compareDiff;

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

			if (DecimalSeparator != null && !Scale.HasValue)
				throw new Exception("Параметр DecimalSeparator применим только когда указан Scale");

			if (CompareDiff != null && !Scale.HasValue)
				throw new Exception("Параметр CompareDiff применим только когда указан Scale");

			NextReadTime = DateTime.MinValue + (readPeriod ?? TimeSpan.FromDays(1));
		}

		// имя топика mqtt
		public string Name { get; private set; }

        // номер регистра
        public ushort Number { get; }

        // тип регистра
        public RegisterType RegType { get; }

        public RegisterFormat RegFormat { get; }

        public ushort? Length { get; }

        public TimeSpan? ReadPeriod { get; }

        public bool WbEvents { get; }

        public double? Scale { get; }

        public int? Precision { get; }

        public string? DecimalSeparator { get; }

		public double? CompareDiff { get; } = null;

        public int SizeInRegisters => (Length ?? 1) * RegFormat.SizeInRegisters();

        public ushort EndRegisterNumber => (ushort)(Number + (ushort)SizeInRegisters);



		int IReadQueueRegister.StartNumber => Number;

		int IReadQueueRegister.EndNumber => EndRegisterNumber;

		public DateTime NextReadTime { get; private set; }

		RegisterType IReadQueueRegister.RegisterType => RegType;

		public event Action? NextReadTimeChanged = null;

		public void SetNextReadTime(DateTime nextReadTime)
		{
			NextReadTime = nextReadTime;
			NextReadTimeChanged?.Invoke();
		}
	}
}
