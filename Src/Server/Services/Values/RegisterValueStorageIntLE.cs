namespace ModbusMqttPublisher.Server.Services.Values
{
    public class RegisterValueStorageIntLE : RegisterValueStorageNumericBase<int>
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
            => RegBitConverter.ReadDataInt32LE(modbusData);

        protected override void WriteToMudbus(Span<ushort> modbusData, int value)
            => RegBitConverter.WriteDataLE(modbusData, value);
    }
}
