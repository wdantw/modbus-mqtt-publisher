using System.IO.Ports;

namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class PortSettings
    {
        public string PortName { get; set; } = string.Empty;

        public PortType PortType { get; set; }

        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;


        public DeviceSettings[] Devices { get; set; } = Array.Empty<DeviceSettings>();

        // максимальконе количество чтения "пустых" одно байтовых регистров в запросе
        // дефолт 7 (13 байт - оверхеда на передачу modbus + 1 на оверхед протокола RS-485)
        public int MaxRegHole { get; set; } = 7;

        // максимальконе количество чтения "пустых" одномитовых регистров в запросе
        public int MaxBitHole { get; set; } = 14 * 8;

        // максимальное количество регистров для чтения в одном запросе
        public int MaxReadRegisters { get; set; } = 125;

        // максимальное количество регистров для чтения в одном запросе
        public int MaxReadBit { get; set; } = 250 * 8;
    }
}
