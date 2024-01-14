namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class DeviceSettings
    {
        public DeviceSettings(byte slaveAddress, int maxRegHole, int maxBitHole, int maxReadRegisters, int maxReadBit, RegisterSettings[] registers)
        {
            SlaveAddress = slaveAddress;
            MaxRegHole = maxRegHole;
            MaxBitHole = maxBitHole;
            MaxReadRegisters = maxReadRegisters;
            MaxReadBit = maxReadBit;
            Registers = registers;
        }

        public byte SlaveAddress { get; }

        // максимальконе количество чтения "пустых" одно байтовых регистров в запросе
        // дефолт 7 (13 байт - оверхеда на передачу modbus + 1 на оверхед протокола RS-485)
        public int MaxRegHole { get; }

        // максимальконе количество чтения "пустых" одномитовых регистров в запросе
        public int MaxBitHole { get; }

        // максимальное количество регистров для чтения в одном запросе
        public int MaxReadRegisters { get; }

        // максимальное количество регистров для чтения в одном запросе
        public int MaxReadBit { get; }

        public RegisterSettings[] Registers { get; }
    }
}
