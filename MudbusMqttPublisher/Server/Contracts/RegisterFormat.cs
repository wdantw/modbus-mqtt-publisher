namespace MudbusMqttPublisher.Server.Contracts
{
    public enum RegisterFormat
    {
        Uint16,
        Uint32,
        Uint64,
        Int16,
        Int32,
        Int64,
        String,
    }

    public static class RegisterFormatExtension
    {
        public static ushort SizeInRegisters(this RegisterFormat format)
        {
            switch (format)
            {
                case RegisterFormat.Uint16:
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
    }
}
