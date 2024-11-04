namespace ModbusMqttPublisher.Server.Contracts.Configs
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

    public static class RegisterTypeExtension
    {
        public static RegisterType GetRegisterType(this ConfigRegisterType regType)
            => (RegisterType)(regType & ConfigRegisterType._TypeMask);
        
        public static RegisterFormat GetRegisterFormat(this ConfigRegisterType regType)
            => (RegisterFormat)((int)(regType & ~ConfigRegisterType._TypeMask) >> (int)ConfigRegisterType._TypeBitCount);


        public static ConfigRegisterType Validate(this ConfigRegisterType regType)
        {
            switch (regType)
            {
                case ConfigRegisterType.Coil:
                case ConfigRegisterType.DiscreteInput:

                case ConfigRegisterType.HoldingRegister:
                case ConfigRegisterType.HoldingRegister32:
                case ConfigRegisterType.HoldingRegister64:
                case ConfigRegisterType.HoldingRegisterS:
                case ConfigRegisterType.HoldingRegisterS32:
                case ConfigRegisterType.HoldingRegisterS64:
                case ConfigRegisterType.HoldingRegisterStr:

                case ConfigRegisterType.InputRegister:
                case ConfigRegisterType.InputRegister32:
                case ConfigRegisterType.InputRegister64:
                case ConfigRegisterType.InputRegisterS:
                case ConfigRegisterType.InputRegisterS32:
                case ConfigRegisterType.InputRegisterS64:
                case ConfigRegisterType.InputRegisterStr:
                    return regType;
                default:
                    throw new ArgumentException($"Указан неверный тип регистра {regType}");
            }

        }

        public static byte SizeInRegisters(this ConfigRegisterType regType, byte? length)
        {
            regType.Validate();
            var type = regType.GetRegisterType().Validate();
            var format = regType.GetRegisterFormat().Validate();

            if (type.IsBitReg())
            {
                if (length.HasValue)
                    throw new ArgumentException("Параметр length не применим к битовым регистрам");

                if (format != RegisterFormat.Default)
                    throw new ArgumentException("Для битовых регистров допустимым форматом является только Default");

                return 1;
            }
            else
            {
                return format.SizeInRegistersNotBitReg(length);
            }
        }
    }

}
