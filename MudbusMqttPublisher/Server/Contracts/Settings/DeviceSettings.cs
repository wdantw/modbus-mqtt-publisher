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
            RegisterSettings[] registers)
        {
            SlaveAddress = slaveAddress;
            MaxRegHole = maxRegHole;
            MaxBitHole = maxBitHole;
            MaxReadRegisters = maxReadRegisters;
            MaxReadBit = maxReadBit;
            MinSleepTimeout = minSleepTimeout;
            Registers = registers;
        }

        public byte SlaveAddress { get; }

        public int MaxRegHole { get; }

        public int MaxBitHole { get; }

        public int MaxReadRegisters { get; }

        public int MaxReadBit { get; }

        public TimeSpan MinSleepTimeout { get; }

        public RegisterSettings[] Registers { get; }
    }
}
