namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class DeviceSettings
    {
		public DeviceSettings(
			byte slaveAddress,
			int maxRegHole,
			int maxBitHole,
			int maxReadRegisters,
			int maxReadBit,
			TimeSpan minSleepTimeout,
			RegisterSettings[] registers,
			TimeSpan errorSleepTimeout,
			TimeSpan readTimeout,
			TimeSpan writeTimeout,
			int writeRetryCount,
			int readRetryCount)
		{
			SlaveAddress = slaveAddress;
			MaxRegHole = maxRegHole;
			MaxBitHole = maxBitHole;
			MaxReadRegisters = maxReadRegisters;
			MaxReadBit = maxReadBit;
			MinSleepTimeout = minSleepTimeout;
			Registers = registers;
			ErrorSleepTimeout = errorSleepTimeout;
			ReadTimeout = readTimeout;
			WriteTimeout = writeTimeout;
			WriteRetryCount = writeRetryCount;
			ReadRetryCount = readRetryCount;
		}

		public byte SlaveAddress { get; }

        public int MaxRegHole { get; }

        public int MaxBitHole { get; }

        public int MaxReadRegisters { get; }

        public int MaxReadBit { get; }

		public TimeSpan MinSleepTimeout { get; }

        public RegisterSettings[] Registers { get; }

		public TimeSpan ErrorSleepTimeout { get; }

		public TimeSpan ReadTimeout { get; }

		public TimeSpan WriteTimeout { get; }

		public int WriteRetryCount { get; }

		public int ReadRetryCount { get; }

	}
}
