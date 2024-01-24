using System.IO.Ports;

namespace MudbusMqttPublisher.Server.Contracts.Settings
{
    public class PortSettings
    {
        public PortSettings(
            string portName,
            int baudRate,
            int dataBits,
            Parity parity,
            StopBits stopBits,
            TimeSpan minSleepTimeout,
            DeviceSettings[] devices)
        {
            SerialName = portName;
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
            MinSleepTimeout = minSleepTimeout;
            Devices = devices;
        }

        public string SerialName { get; }

        public int BaudRate { get; }
        public int DataBits { get; }
        public Parity Parity { get; }
        public StopBits StopBits { get; }
        public TimeSpan MinSleepTimeout { get; }

        public DeviceSettings[] Devices { get; }
    }
}
