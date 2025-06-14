namespace ModbusMqttPublisher.Server.Services.Values
{
    public class RegisterValueStorageULong : RegisterValueStorageNumericBase<ulong>
    {
        protected override double AsDouble(ulong value)
            => value;

        protected override string AsString(ulong value)
            => value.ToString();

        protected override ulong FromDouble(double value)
            => (ulong)Math.Round(value);

        protected override ulong FromString(string value)
            => ulong.Parse(value);

        protected override ulong ReadFromMudbus(ReadOnlySpan<ushort> modbusData)
            => RegBitConverter.ReadDataUInt64LE(modbusData);

        protected override void WriteFromMudbus(Span<ushort> modbusData, ulong value)
            => RegBitConverter.WriteDataLE(modbusData, value);
    }
}
