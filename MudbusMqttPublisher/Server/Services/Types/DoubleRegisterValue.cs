using System.Globalization;
using System.Text;

namespace MudbusMqttPublisher.Server.Services.Types
{
	public class DoubleRegisterValue : IRegisterValue
	{
		double _value;
		double _scale;
		double _compareDiff;
		double _copyDelay;
		int? _precision;
		string _decimalSeparator;
		IRegisterValue _sourceValue;
		IFormatProvider _formatProvider;

		public DoubleRegisterValue(double scale, int? precision, string decimalSeparator, IRegisterValue sourceValue, double? compareDiff)
		{
			_scale = scale;
			_precision = precision;
			_decimalSeparator = decimalSeparator;
			_sourceValue = sourceValue;

			_copyDelay = 0;

			if (compareDiff.HasValue)
			{
				_compareDiff = compareDiff.Value;
			}
			else
			{
				if (_precision.HasValue)
				{
					_copyDelay = 1.0 / Math.Pow(10, _precision.Value);
					_compareDiff = 1.01 * _copyDelay;
				}
				else
				{
					_compareDiff = _scale * 0.01;
				}
			}

			var format = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
			format.NumberDecimalSeparator = _decimalSeparator;
			_formatProvider = format;
		}

		public bool Equals(IPublishValue? other)
		{
			var otherValue = other as DoubleRegisterValue;
			
			if (otherValue == null)
				return false;

			return Math.Abs(_value - otherValue._value) <= _compareDiff;
		}

		private void FromSourceValue()
		{
			_value = _sourceValue.ToDouble() * _scale;
			if (_precision.HasValue)
				_value = Math.Round(_value, _precision.Value);
		}

		private void ToSourceValue()
		{
			_sourceValue.FromDouble(_value / _scale);
		}

		public void FromModbus(ArraySegment<ushort> data)
		{
			_sourceValue.FromModbus(data);
			FromSourceValue();
		}

		public void FromModbus(ArraySegment<bool> data)
		{
			_sourceValue.FromModbus(data);
			FromSourceValue();
		}

		public void ToModbus(ArraySegment<ushort> data)
		{
			ToSourceValue();
			_sourceValue.ToModbus(data);
		}

		public void ToModbus(ArraySegment<bool> data)
		{
			ToSourceValue();
			_sourceValue.ToModbus(data);
		}

		public void FromMqtt(ArraySegment<byte> data)
		{
			_value = double.Parse(Encoding.UTF8.GetString(data), _formatProvider);
		}

		public byte[] ToMqtt()
		{
			return Encoding.UTF8.GetBytes(_value.ToString(_formatProvider));
		}

		public double ToDouble() => _value;

		public void FromDouble(double value)
		{
			_value = value;
		}

		public override string ToString() => Convert.ToString(_value, _formatProvider);

		public void UpdateFrom(IPublishValue value)
		{
			var newValue = ((DoubleRegisterValue)value)._value;
			if (newValue > _value)
				_value = newValue - _copyDelay;
			else
				_value = newValue + _copyDelay;

			if (_precision.HasValue)
				_value = Math.Round(_value, _precision.Value);
		}
	}
}
