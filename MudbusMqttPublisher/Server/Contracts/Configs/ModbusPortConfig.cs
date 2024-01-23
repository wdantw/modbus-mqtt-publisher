using System.IO.Ports;

namespace MudbusMqttPublisher.Server.Contracts.Configs
{
    public class ModbusPortBase : ReadParameters
    {
        public string? Name { get; set; }
        public string? SerialName { get; set; }
        public int? BaudRate { get; set; }
        public int? DataBits { get; set; }
        public Parity? Parity { get; set; }
        public StopBits? StopBits { get; set; }
    }

    public class ModbusPortConfig : ModbusPortBase
    {
        public ModbusDeviceConfig[] Devices { get; set; } = Array.Empty<ModbusDeviceConfig>();

        // переопределение свойств у всех регистров на порту
        public ModbusRegisterCommonParams[] Overrides { get; set; } = Array.Empty<ModbusRegisterCommonParams>();
    }

    public class ModbusPortComplete : ModbusPortBase
    {
        public ModbusDeviceComplete[] Devices { get; set; } = Array.Empty<ModbusDeviceComplete>();
    }

    public class ModbusPorts : List<ModbusPortConfig>
    {
        public const string SectionName = "ModbusPorts";
    }

}
