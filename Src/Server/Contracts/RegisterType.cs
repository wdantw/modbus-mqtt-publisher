namespace MudbusMqttPublisher.Server.Contracts
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

    public static class RegisterTypeMetadata
    {
        public static RegisterType[] Types { get; }

		static RegisterTypeMetadata()
        {
			Types = Enum.GetValues<RegisterType>();
		}
	}

    public static class RegisterTypeExtension
    {
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
