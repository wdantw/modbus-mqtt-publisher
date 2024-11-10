namespace ModbusMqttPublisher.Server.Services.Values
{
    public class RegisterValueStorageLongBE : RegisterValueStorageNumericBase<long>
    {
        protected override double AsDouble(long value)
            => value;

        protected override string AsString(long value)
            => value.ToString();

        protected override long FromDouble(double value)
            => (long)Math.Round(value);

        protected override long FromString(string value)
            => long.Parse(value);

        protected override long ReadFromMudbus(ReadOnlySpan<ushort> modbusData)
            => RegBitConverter.ReadDataInt64BE(modbusData);

        protected override void WriteFromMudbus(Span<ushort> modbusData, long value)
            => RegBitConverter.WriteDataBE(modbusData, value);
    }
}
