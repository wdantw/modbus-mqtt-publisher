namespace ModbusMqttPublisher.Server.Services.Values
{
    public class RegisterValueStorageUInt : INumericRegisterValueStorageWithInConverter
	{
		uint _value;

		public double ToDouble() => _value;

		public bool FromModbus(ReadOnlySpan<ushort> data) => TypeUtils.FromModbus(ref _value, data, BitConverter.ToUInt32);

		public bool FromModbus(ReadOnlySpan<bool> data) => throw new NotImplementedException();

		public byte[] ToMqtt() => MqttStringConverter.ToMqtt(_value.ToString());

		public override string ToString() => _value.ToString();

		public void ToModbus(ReadOnlySpan<byte> mqttData, Span<ushort> modbusData) => TypeUtils.ConvertToModbus(mqttData, modbusData, uint.Parse, BitConverter.TryWriteBytes);

		public void ToModbus(ReadOnlySpan<byte> mqttData, Span<bool> modbusData) => throw new NotImplementedException();

		public void ToModbus(double doubleValue, Span<ushort> modbusData) => TypeUtils.ConvertToModbus((uint)Math.Round(doubleValue), modbusData, BitConverter.TryWriteBytes);
	}
}
