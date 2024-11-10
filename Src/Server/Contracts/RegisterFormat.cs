namespace ModbusMqttPublisher.Server.Contracts
{
    public enum RegisterFormat
    {
        Default, // Uint16 или Bool в зависимости от типа регистра
        Uint32,
        Uint64,
        Uint32BE,
        Uint64BE,
        Int16,
        Int32,
        Int64,
        Int32BE,
        Int64BE,
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
                case RegisterFormat.Uint32BE:
                case RegisterFormat.Uint64BE:
                case RegisterFormat.Int16:
                case RegisterFormat.Int32:
                case RegisterFormat.Int64:
                case RegisterFormat.Int32BE:
                case RegisterFormat.Int64BE:
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
                case RegisterFormat.Uint32BE:
                case RegisterFormat.Int32BE:
                    return 2;
                case RegisterFormat.Uint64:
                case RegisterFormat.Int64:
                case RegisterFormat.Uint64BE:
                case RegisterFormat.Int64BE:
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
