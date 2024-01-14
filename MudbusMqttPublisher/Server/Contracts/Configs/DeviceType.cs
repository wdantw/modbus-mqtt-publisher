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
        
        HoldingRegister = RegisterType.HoldingRegister | RegisterFormat.Uint16 << _TypeBitCount,
        InputRegister   = RegisterType.InputRegister | RegisterFormat.Uint16 << _TypeBitCount,

        HoldingRegister32   = RegisterType.HoldingRegister | RegisterFormat.Uint32 << _TypeBitCount,
        HoldingRegister64   = RegisterType.HoldingRegister | RegisterFormat.Uint64 << _TypeBitCount,
        HoldingRegisterS    = RegisterType.HoldingRegister | RegisterFormat.Int16 << _TypeBitCount,
        HoldingRegisterS32  = RegisterType.HoldingRegister | RegisterFormat.Int32 << _TypeBitCount,
        HoldingRegisterS64  = RegisterType.HoldingRegister | RegisterFormat.Int64 << _TypeBitCount,
        HoldingRegisterStr  = RegisterType.HoldingRegister | RegisterFormat.String << _TypeBitCount,


        InputRegister32     = RegisterType.InputRegister | RegisterFormat.Uint32 << _TypeBitCount,
        InputRegister64     = RegisterType.InputRegister | RegisterFormat.Uint64 << _TypeBitCount,
        InputRegisterS      = RegisterType.InputRegister | RegisterFormat.Int16 << _TypeBitCount,
        InputRegisterS32    = RegisterType.InputRegister | RegisterFormat.Int32 << _TypeBitCount,
        InputRegisterS64    = RegisterType.InputRegister | RegisterFormat.Int64 << _TypeBitCount,
        InputRegisterStr    = RegisterType.InputRegister | RegisterFormat.String << _TypeBitCount,
    }

    public static class RegisterTypeExtension
    {
        public static RegisterType GetRegisterType(this ConfigRegisterType regType)
            => (RegisterType)(regType & ConfigRegisterType._TypeMask);
        public static RegisterFormat GetRegisterFormat(this ConfigRegisterType regType)
            => (RegisterFormat)((int)(regType & ~ConfigRegisterType._TypeMask) >> (int)ConfigRegisterType._TypeBitCount);
    }

    public class ModbusDeviceRegister
    {
        public ConfigRegisterType? RegType { get; set; }

        public ushort? Length { get; set; }
        public double? Scale { get; set; }

        public ushort? Number { get; set; }
        public ushort? NumberStart { get; set; }
        public ushort? NumberCount { get; set; }
        public ushort? ColCount { get; set; }
        public ushort? ColLength { get; set; }
        public ushort? RowCount { get; set; }

        public string? Name { get; set; }
        public string? Names { get; set; }
        public bool? WbEvents { get; set; }

        public TimeSpan? ReadPeriod { get; set; }
    }
}
