
namespace ModbusMqttPublisher.Server.Services.Values
{
    public class RegisterValueStorageUShort : RegisterValueStorageNumericBase<ushort>
    {
        protected override double AsDouble(ushort value)
            => value;

        protected override string AsString(ushort value)
            => value.ToString();

        protected override ushort FromDouble(double value)
            => (ushort)Math.Round(value);

        protected override ushort FromString(string value)
            => ushort.Parse(value);

        protected override ushort ReadFromMudbus(ReadOnlySpan<ushort> modbusData)
            => RegBitConverter.ReadDataUInt16(modbusData);

        protected override void WriteToMudbus(Span<ushort> modbusData, ushort value)
            => RegBitConverter.WriteData(modbusData, value);
    }
}
