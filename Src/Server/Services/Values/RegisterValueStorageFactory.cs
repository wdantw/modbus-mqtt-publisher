using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Services.Values
{
    public static class RegisterValueStorageFactory
    {
        private static INumericRegisterValueStorageWithInConverter CreateNumeric(RegisterFormat format)
        {
            switch (format)
            {
                case RegisterFormat.Default:
                    return new RegisterValueStorageUShort();

                case RegisterFormat.Uint32:
                    return new RegisterValueStorageUInt();

                case RegisterFormat.Uint32BE:
                    return new RegisterValueStorageUIntBE();

                case RegisterFormat.Uint64:
                    return new RegisterValueStorageULong();

                case RegisterFormat.Uint64BE:
                    return new RegisterValueStorageULongBE();

                case RegisterFormat.Int16:
                    return new RegisterValueStorageShort();

                case RegisterFormat.Int32:
                    return new RegisterValueStorageInt();

                case RegisterFormat.Int32BE:
                    return new RegisterValueStorageIntBE();

                case RegisterFormat.Int64:
                    return new RegisterValueStorageLong();

                case RegisterFormat.Int64BE:
                    return new RegisterValueStorageLongBE();

                default:
                    throw new ArgumentException($"Недопустимый формат для целочисленного регистра {format}");
            }
        }

        public static IRegisterValueStorageWithInConverter Create(
            double? scale,
            int? precision,
            string? decimalSeparator,
            double? compareDiff,
            RegisterType registerType,
            RegisterFormat registerFormat)
        {
            if (scale.HasValue)
            {
                if (registerFormat == RegisterFormat.String)
                    throw new Exception("Параметр Scale неприменим к строкам");

                return new RegisterValueStorageDouble(
                    scale.Value,
                    precision,
                    decimalSeparator ?? DefaultSettings.DecimalSeparator,
                    CreateNumeric(registerFormat),
                    compareDiff
                    );
            }
            else
            {
                if (precision.HasValue)
                    throw new Exception("Параметр Precision применим только когда указан Scale");

                if (!string.IsNullOrWhiteSpace(decimalSeparator))
                    throw new Exception("Параметр DecimalSeparator применим только когда указан Scale");

                if (compareDiff.HasValue)
                    throw new Exception("Параметр CompareDiff применим только когда указан Scale");


                if (registerType.IsBitReg())
                {
                    return new RegisterValueStorageBool();
                }
                else
                {
                    if (registerFormat == RegisterFormat.String)
                    {
                        return new RegisterValueStorageString();
                    }
                    else
                    {
                        return CreateNumeric(registerFormat);
                    }
                }
            }
        }
    }
}
