using ModbusMqttPublisher.Server.Services.Queues;
using ModbusMqttPublisher.Server.Services.Values;

namespace ModbusMqttPublisher.Server.Contracts.Settings
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
			double? compareDiff,
			bool forcePublish)
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
			ForcePublish = forcePublish;

			registerValue = RegisterValueStorageFactory.Create(this);
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

		public bool ForcePublish { get; }

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

		private IRegisterValueStorageWithInConverter registerValue;

		public IPublishValueSorage PublishValue => registerValue;

		public IIncomeRegisterConverter IncomeValueConverter => registerValue;

		private bool isFirstWrite = true;

        public bool ReadFromModbus(DateTime readTime, ReadOnlySpan<ushort> data)
		{
			SetNextReadTime(ReadPeriod.HasValue ? (readTime + ReadPeriod.Value) : DateTime.MaxValue);
			var res = registerValue.FromModbus(data) || isFirstWrite;
			isFirstWrite = false;
			return res;
		}

		public bool ReadFromModbus(DateTime readTime, ReadOnlySpan<bool> data)
		{
			SetNextReadTime(ReadPeriod.HasValue ? (readTime + ReadPeriod.Value) : DateTime.MaxValue);
			var res = registerValue.FromModbus(data) || isFirstWrite;
			isFirstWrite = false;
			return res;
		}
	}
}
