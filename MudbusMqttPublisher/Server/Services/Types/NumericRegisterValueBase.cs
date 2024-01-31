using System.Text;

namespace MudbusMqttPublisher.Server.Services.Types
{
	public abstract class NumericRegisterValueBase<T> : IRegisterValue
		where T : struct, IEquatable<T>
	{
		protected T _value = default;

		protected abstract T ValueFromBytes(ReadOnlySpan<byte> data);
		protected abstract byte[] ValueToBytes(T data);
		protected abstract T ValueFromString(string data);
		protected abstract string ValueToString(T data);
		public abstract double ToDouble();
		public abstract void FromDouble(double value);

		public bool Equals(IPublishValue? other)
		{
			var commpared = other as NumericRegisterValueBase<T>;
			return commpared != null && _value.Equals(commpared._value);
		}

		public void FromModbus(ArraySegment<ushort> data)
		{
			_value = ValueFromBytes(TypeUtils.ToBytes(data));
		}

		public void ToModbus(ArraySegment<ushort> data)
		{
			TypeUtils.FromBytes(ValueToBytes(_value), data);
		}

		public void FromModbus(ArraySegment<bool> data)
		{
			throw new NotImplementedException("Данный тип значения не совместим с данным типом регистра");
		}

		public void ToModbus(ArraySegment<bool> data)
		{
			throw new NotImplementedException("Данный тип значения не совместим с данным типом регистра");
		}

		public void FromMqtt(ArraySegment<byte> data)
		{
			_value = ValueFromString(Encoding.UTF8.GetString(data));
		}

		public byte[] ToMqtt()
		{
			return Encoding.UTF8.GetBytes(ValueToString(_value));
		}

		public override string ToString() => ValueToString(_value);

		public void UpdateFrom(IPublishValue value)
		{
			_value = ((NumericRegisterValueBase<T>)value)._value;
		}

	}
}
