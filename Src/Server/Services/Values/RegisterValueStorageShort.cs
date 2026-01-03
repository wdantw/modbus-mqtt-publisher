namespace ModbusMqttPublisher.Server.Services.Values
{

    public class RegisterValueStorageShort : RegisterValueStorageNumericBase<short>
    {
        protected override double AsDouble(short value)
            => value;

        protected override string AsString(short value)
            => value.ToString();

        protected override short FromDouble(double value)
            => (short)Math.Round(value);

        protected override short FromString(string value)
            => short.Parse(value);

        protected override short ReadFromMudbus(ReadOnlySpan<ushort> modbusData)
            => RegBitConverter.ReadDataInt16(modbusData);

        protected override void WriteToMudbus(Span<ushort> modbusData, short value)
            => RegBitConverter.WriteData(modbusData, value);
    }
}
