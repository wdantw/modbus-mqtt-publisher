namespace ModbusMqttPublisher.Server.Services.Values
{
	public class RegisterValueStorageDouble : IRegisterValueStorageWithInConverter
	{
		double _value;
		double _scale;
		double _compareDiff;
		int? _precision;
		INumericRegisterValueStorageWithInConverter _sourceValue;
		IFormatProvider _formatProvider;

		public RegisterValueStorageDouble(double scale, int? precision, string decimalSeparator, INumericRegisterValueStorageWithInConverter sourceValue, double? compareDiff)
		{
			_scale = scale;
			_precision = precision;
			_sourceValue = sourceValue;

			if (compareDiff.HasValue)
			{
				_compareDiff = compareDiff.Value;
			}
			else
			{
				if (_precision.HasValue)
				{
					_compareDiff = 1.01 / Math.Pow(10, _precision.Value);
				}
				else
				{
					_compareDiff = _scale * 0.01;
				}
			}

			_formatProvider = NumberFormatHelper.CreateNumberFormatProvider(decimalSeparator);
		}

		private bool FromSourceValue()
		{
			var newValue = _sourceValue.ToDouble() * _scale;
			if (_precision.HasValue)
				newValue = Math.Round(newValue, _precision.Value);

			if (Math.Abs(_value - newValue) <= _compareDiff)
				return false;

			_value = newValue;

			return true;
		}

		public bool FromModbus(ReadOnlySpan<ushort> data)
		{
			_sourceValue.FromModbus(data);
			return FromSourceValue();
		}

		public bool FromModbus(ReadOnlySpan<bool> data)
		{
			_sourceValue.FromModbus(data);
			return FromSourceValue();
		}

		public byte[] ToMqtt()
		{
			return MqttStringConverter.ToMqtt(_value.ToString(_formatProvider));
		}

		public override string ToString() => _value.ToString(_formatProvider);

		public void ToModbus(ReadOnlySpan<byte> mqttData, Span<ushort> modbusData)
		{
			var strValue = MqttStringConverter.FromMqtt(mqttData);
			var doubleValue = double.Parse(strValue, _formatProvider);
			_sourceValue.ToModbus(doubleValue / _scale, modbusData);
		}

		public void ToModbus(ReadOnlySpan<byte> mqttData, Span<bool> modbusData) => throw new NotImplementedException();
	}
}
