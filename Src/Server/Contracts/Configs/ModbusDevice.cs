namespace ModbusMqttPublisher.Server.Contracts.Configs
{
    public class ModbusDeviceBaseShared : ReadParameters
    {
        public string? Name { get; set; } = string.Empty;
        public string? ParentTypeName { get; set; }
    }

    public class ModbusDeviceConfigBase : ModbusDeviceBaseShared
    {
        public ModbusRegisterTemplate[] Registers { get; set; } = Array.Empty<ModbusRegisterTemplate>();
        public ModbusRegisterModifier[] Modifiers { get; set; } = Array.Empty<ModbusRegisterModifier>();
    }

    public class ModbusDeviceConfig : ModbusDeviceConfigBase
    {
        public byte? SlaveAddress { get; set; }
    }

    public class ModbusDeviceTypeConfig : ModbusDeviceConfigBase
    {
        public string? TypeName { get; set; }
    }

    public class ModbusDeviceTypes : List<ModbusDeviceTypeConfig>
    {
        public const string SectionName = "ModbusDeviceTypes";
    }

    public class ModbusDeviceCompleteBase : ModbusDeviceBaseShared
    {
        public List<ModbusRegisterCompleted> Registers { get; set; } = new ();
    }

    public class ModbusDeviceComplete : ModbusDeviceCompleteBase
    {
        public byte? SlaveAddress { get; set; }
    }

}
