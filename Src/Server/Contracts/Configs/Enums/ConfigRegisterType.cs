namespace ModbusMqttPublisher.Server.Contracts.Configs.Enums
{
    public enum ConfigRegisterType
    {
        _TypeBitCount = 2,
        _TypeMask = (1 << _TypeBitCount) - 1,

        Coil = RegisterType.Coil,
        DiscreteInput = RegisterType.DiscreteInput,

        HoldingRegister = RegisterType.HoldingRegister | RegisterFormat.Default << _TypeBitCount,
        HoldingRegister32 = RegisterType.HoldingRegister | RegisterFormat.Uint32 << _TypeBitCount,
        HoldingRegister64 = RegisterType.HoldingRegister | RegisterFormat.Uint64 << _TypeBitCount,
        HoldingRegisterS = RegisterType.HoldingRegister | RegisterFormat.Int16 << _TypeBitCount,
        HoldingRegisterS32 = RegisterType.HoldingRegister | RegisterFormat.Int32 << _TypeBitCount,
        HoldingRegisterS64 = RegisterType.HoldingRegister | RegisterFormat.Int64 << _TypeBitCount,
        HoldingRegisterStr = RegisterType.HoldingRegister | RegisterFormat.String << _TypeBitCount,

        InputRegister = RegisterType.InputRegister | RegisterFormat.Default << _TypeBitCount,
        InputRegister32 = RegisterType.InputRegister | RegisterFormat.Uint32 << _TypeBitCount,
        InputRegister64 = RegisterType.InputRegister | RegisterFormat.Uint64 << _TypeBitCount,
        InputRegisterS = RegisterType.InputRegister | RegisterFormat.Int16 << _TypeBitCount,
        InputRegisterS32 = RegisterType.InputRegister | RegisterFormat.Int32 << _TypeBitCount,
        InputRegisterS64 = RegisterType.InputRegister | RegisterFormat.Int64 << _TypeBitCount,
        InputRegisterStr = RegisterType.InputRegister | RegisterFormat.String << _TypeBitCount,
    }
}
