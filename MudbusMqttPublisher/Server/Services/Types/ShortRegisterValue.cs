namespace MudbusMqttPublisher.Server.Services.Types
{
	public class ShortRegisterValue : NumericRegisterValueBase<short>
	{
		protected override short ValueFromBytes(ReadOnlySpan<byte> data) => BitConverter.ToInt16(data);

		protected override short ValueFromString(string data) => Convert.ToInt16(data);

		protected override byte[] ValueToBytes(short data) => BitConverter.GetBytes(data);

		protected override string ValueToString(short data) => Convert.ToString(data);

		public override double ToDouble() => Value;

		public override void FromDouble(double value) => Value = (short)value;
	}
}
