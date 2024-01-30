using System.Text;

namespace MudbusMqttPublisher.Server.Services.Types
{
	public class StringRegisterValue : IRegisterValue
	{
		string _value = string.Empty;

		public bool Equals(IRegisterValue? other)
		{
			var commpared = other as StringRegisterValue;
			return string.Equals(_value, commpared?._value ?? string.Empty);
		}

		public void FromModbus(ArraySegment<ushort> data)
		{
			_value = Encoding.ASCII.GetString(TypeUtils.ToBytes(data).Where(b => b != 0).ToArray()) ?? string.Empty;
		}

		public void FromModbus(ArraySegment<bool> data)
		{
			throw new NotImplementedException();
		}

		public void ToModbus(ArraySegment<ushort> data)
		{
			var bytes = new byte[data.Count * 2];
			Encoding.ASCII.GetBytes(_value, bytes);
			TypeUtils.FromBytes(bytes, data);
		}

		public void ToModbus(ArraySegment<bool> data)
		{
			throw new NotImplementedException();
		}

		public void FromMqtt(ArraySegment<byte> data)
		{
			_value = Encoding.UTF8.GetString(data);
		}

		public byte[] ToMqtt()
		{
			return Encoding.UTF8.GetBytes(_value);
		}

		public void FromDouble(double value)
		{
			throw new NotImplementedException();
		}

		public double ToDouble()
		{
			throw new NotImplementedException();
		}

		public override string ToString() => _value;

		public void UpdateFrom(IRegisterValue value)
		{
			_value = ((StringRegisterValue)value)._value;
		}

	}
}
