namespace ModbusMqttPublisher.Server.Services.Types
{
	public class IntRegisterValue : NumericRegisterValueBase<int>
	{
		protected override int ValueFromBytes(ReadOnlySpan<byte> data) => BitConverter.ToInt32(data);

		protected override int ValueFromString(string data) => Convert.ToInt32(data);

		protected override byte[] ValueToBytes(int data) => BitConverter.GetBytes(data);

		protected override string ValueToString(int data) => Convert.ToString(data);

		public override double ToDouble() => _value;

		public override void FromDouble(double value) => TypeUtils.FromDouble(ref _value, value);
	}
}
