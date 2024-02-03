namespace ModbusMqttPublisher.Server.Services.Types
{
	public class UintRegisterValue : NumericRegisterValueBase<uint>
	{
		protected override uint ValueFromBytes(ReadOnlySpan<byte> data) => BitConverter.ToUInt32(data);

		protected override uint ValueFromString(string data) => Convert.ToUInt32(data);

		protected override byte[] ValueToBytes(uint data) => BitConverter.GetBytes(data);

		protected override string ValueToString(uint data) => Convert.ToString(data);

		public override double ToDouble() => _value;

		public override void FromDouble(double value) => TypeUtils.FromDouble(ref _value, value);
	}
}
