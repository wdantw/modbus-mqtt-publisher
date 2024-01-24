using System.IO.Ports;

namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public static class DefaultSettings
    {
        public static int BaudRate => 9600;
        public static int DataBits => 8;
        public static Parity Parity => Parity.None;
        public static StopBits StopBits => StopBits.One;
        public static TimeSpan MinSleepTimeout(int baudRate) => TimeSpan.FromMilliseconds(Math.Ceiling(38500000.0 / baudRate) / 1000.0);
        public static bool AllowWbEvents => false;

        // максимальконе количество чтения "пустых" одно байтовых регистров в запросе
        // дефолт 7 (13 байт - оверхеда на передачу modbus + 1 на оверхед протокола RS-485)
        public static int MaxRegHole => 7;

        // максимальконе количество чтения "пустых" одномитовых регистров в запросе
        public static int MaxBitHole => 14 * 8;

        // максимальное количество регистров для чтения в одном запросе (ограничение протокола)
        public static int MaxReadRegisters => 125;

        // максимальное количество регистров для чтения в одном запросе (ограничение протокола)
        public static int MaxReadBit => 250 * 8;
    }
}
