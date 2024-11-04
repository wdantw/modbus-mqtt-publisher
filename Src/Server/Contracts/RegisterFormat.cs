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
        public static RegisterFormat Validate(this RegisterFormat format)
        {
            switch (format)
            {
                case RegisterFormat.Default:
                case RegisterFormat.Uint32:
                case RegisterFormat.Uint64:
                case RegisterFormat.Int16:
                case RegisterFormat.Int32:
                case RegisterFormat.Int64:
                case RegisterFormat.String:
                    return format;
                default:
                    throw new ArgumentException($"Недопустимый формат регистра {format}");
            }
        }

        public static byte SizeInRegistersNotBitReg(this RegisterFormat format, byte? length)
        {
            if (format == RegisterFormat.String)
            {
                if (!length.HasValue)
                    throw new ArgumentException("Не указана длина строки для строкового регистра");

                return length.Value;
            }

            if (length.HasValue)
                throw new ArgumentException("Указана длина строки для нестрокового регистра");

            switch (format)
            {
                case RegisterFormat.Default:
                case RegisterFormat.Int16:
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
