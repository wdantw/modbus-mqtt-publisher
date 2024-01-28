using System.Text;

namespace MudbusMqttPublisher.Server.Services.Types
{
	public class UlongRegisterValue : NumericRegisterValueBase<ulong>
	{
		protected override ulong ValueFromBytes(ReadOnlySpan<byte> data) => BitConverter.ToUInt64(data);

		protected override ulong ValueFromString(string data) => Convert.ToUInt64(data);

		protected override byte[] ValueToBytes(ulong data) => BitConverter.GetBytes(data);

		protected override string ValueToString(ulong data) => Convert.ToString(data);

		public override double ToDouble() => _value;

		public override void FromDouble(double value) => TypeUtils.FromDouble(ref _value, value);
	}
}
