namespace MudbusMqttPublisher.Server.Contracts.Configs
{
    public class ModbudsDeviceType
    {
        public string? TypeName { get; set; }
        public string? ParentTypeName { get; set; }

        public ModbusDeviceRegister[] Registers { get; set; } = Array.Empty<ModbusDeviceRegister>();
    }

    public enum ConfigRegisterType
    {
        _TypeBitCount = 2,
        _TypeMask = (1 << _TypeBitCount) - 1,

        Coil            = RegisterType.Coil,
        DiscreteInput   = RegisterType.DiscreteInput,
        
        HoldingRegister = RegisterType.HoldingRegister | ConfigRegisterFormat.Uint16 << _TypeBitCount,
        InputRegister   = RegisterType.InputRegister | ConfigRegisterFormat.Uint16 << _TypeBitCount,

        HoldingRegister32   = RegisterType.HoldingRegister | ConfigRegisterFormat.Uint32 << _TypeBitCount,
        HoldingRegister64   = RegisterType.HoldingRegister | ConfigRegisterFormat.Uint64 << _TypeBitCount,
        HoldingRegisterS    = RegisterType.HoldingRegister | ConfigRegisterFormat.Int16 << _TypeBitCount,
        HoldingRegisterS32  = RegisterType.HoldingRegister | ConfigRegisterFormat.Int32 << _TypeBitCount,
        HoldingRegisterS64  = RegisterType.HoldingRegister | ConfigRegisterFormat.Int64 << _TypeBitCount,
        HoldingRegisterStr  = RegisterType.HoldingRegister | ConfigRegisterFormat.String << _TypeBitCount,


        InputRegister32     = RegisterType.InputRegister | ConfigRegisterFormat.Uint32 << _TypeBitCount,
        InputRegister64     = RegisterType.InputRegister | ConfigRegisterFormat.Uint64 << _TypeBitCount,
        InputRegisterS      = RegisterType.InputRegister | ConfigRegisterFormat.Int16 << _TypeBitCount,
        InputRegisterS32    = RegisterType.InputRegister | ConfigRegisterFormat.Int32 << _TypeBitCount,
        InputRegisterS64    = RegisterType.InputRegister | ConfigRegisterFormat.Int64 << _TypeBitCount,
        InputRegisterStr    = RegisterType.InputRegister | ConfigRegisterFormat.String << _TypeBitCount,
    }

    public static class RegisterTypeExtension
    {
        public static RegisterType GetRegisterType(this ConfigRegisterType regType)
            => (RegisterType)(regType & ConfigRegisterType._TypeMask);
        public static ConfigRegisterFormat GetRegisterFormat(this ConfigRegisterType regType)
            => (ConfigRegisterFormat)((int)(regType & ~ConfigRegisterType._TypeMask) >> (int)ConfigRegisterType._TypeBitCount);
    }

    public class ModbusDeviceRegister
    {
        public ConfigRegisterType? RegType { get; set; }

        public int? Length { get; set; }
        public double? Scale { get; set; }

        public int? Number { get; set; }
        public int? NumberStart { get; set; }
        public int? NumberCount { get; set; }
        public int? ColCount { get; set; }
        public int? ColLength { get; set; }
        public int? RowCount { get; set; }

        public string? Name { get; set; }
        public string? Names { get; set; }
        public bool WbEvents { get; set; } = false;
    }
}
