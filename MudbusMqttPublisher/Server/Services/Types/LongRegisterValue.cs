namespace MudbusMqttPublisher.Server.Services.Types
{
	public class LongRegisterValue : NumericRegisterValueBase<long>
	{
		protected override long ValueFromBytes(ReadOnlySpan<byte> data) => BitConverter.ToInt64(data);

		protected override long ValueFromString(string data) => Convert.ToInt64(data);

		protected override byte[] ValueToBytes(long data) => BitConverter.GetBytes(data);

		protected override string ValueToString(long data) => Convert.ToString(data);

		public override double ToDouble() => Value;

		public override void FromDouble(double value) => Value = (long)value;
	}
}
