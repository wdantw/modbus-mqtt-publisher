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

                case RegisterFormat.Uint64:
                    return new RegisterValueStorageULong();

                case RegisterFormat.Int16:
                    return new RegisterValueStorageShort();

                case RegisterFormat.Int32:
                    return new RegisterValueStorageInt();

                case RegisterFormat.Int64:
                    return new RegisterValueStorageLong();

                default:
                    throw new ArgumentException($"Недопустимый формат для целочисленного регистра {format}");
            }
        }

        public static IRegisterValueStorageWithInConverter Create(RegisterSettings register)
        {
            if (register.Scale.HasValue)
            {
                return new RegisterValueStorageDouble(
                    register.Scale.Value,
                    register.Precision,
                    register.DecimalSeparator ?? DefaultSettings.DecimalSeparator,
                    CreateNumeric(register.RegFormat),
                    register.CompareDiff
                    );
            }
            else
            {
                if (register.RegType.IsBitReg())
                {
                    return new RegisterValueStorageBool();
                }
                else
                {
                    if (register.RegFormat == RegisterFormat.String)
                    {
                        return new RegisterValueStorageString();
                    }
                    else
                    {
                        return CreateNumeric(register.RegFormat);
                    }
                }
            }
        }
    }
}
