namespace ModbusMqttPublisher.Server.Contracts
{
    public enum RegisterType
    {
        // Coil bit R/W
        Coil,

        // Input Bit R-Only
        DiscreteInput,

		// Holding U16 R/W
		HoldingRegister,

        // Input Register U16 R-Only
        InputRegister
    }

    public static class RegisterTypeExtension
    {
        public static RegisterType Validate(this RegisterType type)
        {
            switch (type)
            {
                case RegisterType.Coil:
                case RegisterType.DiscreteInput:
                case RegisterType.HoldingRegister:
                case RegisterType.InputRegister:
                    return type;
                default:
                    throw new ArgumentException($"Недоступсимый тип регистра {type}");
            }
        }

        public static bool IsBitReg(this RegisterType type)
        {
            switch (type)
            {
                case RegisterType.Coil:
                case RegisterType.DiscreteInput:
                    return true;
                default:
                    return false;
            }
        }

        public static bool AllowWrite(this RegisterType type)
        {
            switch (type)
            {
                case RegisterType.Coil:
                case RegisterType.HoldingRegister:
                    return true;
                default:
                    return false;
            }
        }
    }
}
