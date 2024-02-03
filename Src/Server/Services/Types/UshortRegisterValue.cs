namespace ModbusMqttPublisher.Server.Services.Types
{
	public class UshortRegisterValue : NumericRegisterValueBase<ushort>
	{
		protected override ushort ValueFromBytes(ReadOnlySpan<byte> data) => BitConverter.ToUInt16(data);

		protected override ushort ValueFromString(string data) => Convert.ToUInt16(data);

		protected override byte[] ValueToBytes(ushort data) => BitConverter.GetBytes(data);

		protected override string ValueToString(ushort data) => Convert.ToString(data);

		public override double ToDouble() => _value;

		public override void FromDouble(double value) => TypeUtils.FromDouble(ref _value, value);
	}
}
