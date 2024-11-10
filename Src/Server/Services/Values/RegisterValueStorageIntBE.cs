namespace ModbusMqttPublisher.Server.Services.Values
{
    public class RegisterValueStorageIntBE : RegisterValueStorageNumericBase<int>
    {
        protected override double AsDouble(int value)
            => value;

        protected override string AsString(int value)
            => value.ToString();

        protected override int FromDouble(double value)
            => (int)Math.Round(value);

        protected override int FromString(string value)
            => int.Parse(value);

        protected override int ReadFromMudbus(ReadOnlySpan<ushort> modbusData)
            => RegBitConverter.ReadDataInt32BE(modbusData);

        protected override void WriteFromMudbus(Span<ushort> modbusData, int value)
            => RegBitConverter.WriteDataBE(modbusData, value);
    }
}
