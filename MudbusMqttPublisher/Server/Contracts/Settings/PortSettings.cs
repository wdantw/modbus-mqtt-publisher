using System.IO.Ports;

namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class PortSettings
    {
        public PortSettings(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, TimeSpan minSleepTimeout, DeviceSettings[] devices)
        {
            PortName = portName;
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
            MinSleepTimeout = minSleepTimeout;
            Devices = devices;
        }

        public string PortName { get; set; } = string.Empty;

        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public TimeSpan MinSleepTimeout { get; set;} = TimeSpan.FromMilliseconds(4.011);


        public DeviceSettings[] Devices { get; set; } = Array.Empty<DeviceSettings>();
    }
}
