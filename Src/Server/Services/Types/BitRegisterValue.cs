using System.Text;

namespace MudbusMqttPublisher.Server.Services.Types
{
	public class BitRegisterValue : IRegisterValue
	{
		private bool _value;

		public bool Equals(IPublishValue? other)
		{
			var commpared = other as BitRegisterValue;
			return commpared != null && _value == commpared._value;
		}

		public void FromModbus(ArraySegment<ushort> data)
		{
			throw new NotImplementedException("Данный тип значения не совместим с данным типом регистра");
		}

		public void ToModbus(ArraySegment<ushort> data)
		{
			throw new NotImplementedException("Данный тип значения не совместим с данным типом регистра");
		}

		public void FromModbus(ArraySegment<bool> data)
		{
			_value = data[0];
		}

		public void ToModbus(ArraySegment<bool> data)
		{
			data[0] = _value;
		}

		public void FromMqtt(ArraySegment<byte> data)
		{
			_value = Convert.ToBoolean(Encoding.UTF8.GetString(data));
		}

		public byte[] ToMqtt() => Encoding.UTF8.GetBytes(Convert.ToString(_value));

		public double ToDouble() => Convert.ToDouble(_value);

		public void FromDouble(double value)
		{
			TypeUtils.FromDouble(ref _value, value);
		}

		public override string ToString() => Convert.ToString(_value);

		public void UpdateFrom(IPublishValue value)
		{
			_value = ((BitRegisterValue)value)._value;
		}
	}
}
