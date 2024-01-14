namespace MudbusMqttPublisher.Server.Contracts
{
    public enum RegisterType
    {
        Coil, // Coil bit R/W
        DiscreteInput, // Input Bit R-Only
        HoldingRegister, // Holding U16 R/W
        InputRegister // Input Register U16 R-Only
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
