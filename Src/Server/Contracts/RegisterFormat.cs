namespace ModbusMqttPublisher.Server.Contracts
{
    public enum RegisterFormat
    {
        Default, // Uint16 или Bool в зависимости от типа регистра
        Uint32,
        Uint64,
        Int16,
        Int32,
        Int64,
        String,
    }

    public static class RegisterFormatExtension
    {
        public static byte SizeInRegisters(this RegisterFormat format)
        {
            switch (format)
            {
                case RegisterFormat.Default:
                case RegisterFormat.Int16:
                case RegisterFormat.String:
                    return 1;
                case RegisterFormat.Uint32:
                case RegisterFormat.Int32:
                    return 2;
                case RegisterFormat.Uint64:
                case RegisterFormat.Int64:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        public static bool FormatHasLength(this RegisterFormat format)
        {
            return format == RegisterFormat.String;
        }
    }
}
