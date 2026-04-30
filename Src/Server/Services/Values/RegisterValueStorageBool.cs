namespace ModbusMqttPublisher.Server.Services.Values
{
    public class RegisterValueStorageBool : IRegisterValueStorageWithInConverter
	{
		bool _value;

		public bool FromModbus(ReadOnlySpan<ushort> data) => throw new NotImplementedException();

		public bool FromModbus(ReadOnlySpan<bool> data)
		{
			var newValue = data[0];

			if (newValue == _value)
				return false;

			_value = newValue;

			return true;
		}

		public byte[] ToMqtt() => MqttStringConverter.ToMqtt(_value.ToString());

		public override string ToString() => _value.ToString();


		public void ToModbus(ReadOnlySpan<byte> mqttData, Span<ushort> modbusData) => throw new NotImplementedException();

		public void ToModbus(ReadOnlySpan<byte> mqttData, Span<bool> modbusData)
		{
			var strData = MqttStringConverter.FromMqtt(mqttData);
			var incomeValue = bool.Parse(strData);
			modbusData[0] = incomeValue;
		}
	}
}
