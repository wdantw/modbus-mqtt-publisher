using System.IO.Ports;

namespace MudbusMqttPublisher.Server.Contracts.Configs
{
    public class ModbusPort
    {
        public string? SerialName { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public TimeSpan MinSleepTimeout { get; set; } = TimeSpan.FromMilliseconds(4.011);
        public bool AllowWbEvents { get; set; } = false;

        // максимальконе количество чтения "пустых" одно байтовых регистров в запросе
        // дефолт 7 (13 байт - оверхеда на передачу modbus + 1 на оверхед протокола RS-485)
        public int MaxRegHole { get; set; } = 7;

        // максимальконе количество чтения "пустых" одномитовых регистров в запросе
        public int MaxBitHole { get; set; } = 14 * 8;

        // максимальное количество регистров для чтения в одном запросе
        public int MaxReadRegisters { get; set; } = 125;

        // максимальное количество регистров для чтения в одном запросе
        public int MaxReadBit { get; set; } = 250 * 8;

        public ModbusDevice[] Devices { get; set; } = Array.Empty<ModbusDevice>();
    }

    public class ModbusDevice
    {
        public byte? SlaveAddress { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DeviceTypeName { get; set; }
        public int? MaxRegHole { get; set; }
        public int? MaxBitHole { get; set; }
        public int? MaxReadRegisters { get; set; }
        public int? MaxReadBit { get; set; }
        public ModbusDeviceRegister[] Registers { get; set; } = Array.Empty<ModbusDeviceRegister>();
    }
}
