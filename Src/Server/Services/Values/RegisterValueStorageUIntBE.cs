namespace ModbusMqttPublisher.Server.Services.Values
{
    public class RegisterValueStorageUIntBE : RegisterValueStorageNumericBase<uint>
    {
        protected override double AsDouble(uint value)
            => value;

        protected override string AsString(uint value)
            => value.ToString();

        protected override uint FromDouble(double value)
            => (uint)Math.Round(value);

        protected override uint FromString(string value)
            => uint.Parse(value);

        protected override uint ReadFromMudbus(ReadOnlySpan<ushort> modbusData)
            => RegBitConverter.ReadDataUInt32BE(modbusData);

        protected override void WriteFromMudbus(Span<ushort> modbusData, uint value)
            => RegBitConverter.WriteDataBE(modbusData, value);
    }
}
