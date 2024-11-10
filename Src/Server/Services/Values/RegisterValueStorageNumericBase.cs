namespace ModbusMqttPublisher.Server.Services.Values
{
    public abstract class RegisterValueStorageNumericBase<TValue> : INumericRegisterValueStorageWithInConverter
        where TValue : struct
    {
        protected TValue _value;

        protected abstract string AsString(TValue value);
        protected abstract double AsDouble(TValue value);
        protected abstract TValue FromString(string value);
        protected abstract TValue FromDouble(double value);
        protected abstract TValue ReadFromMudbus(ReadOnlySpan<ushort> modbusData);
        protected abstract void WriteFromMudbus(Span<ushort> modbusData, TValue value);

        public double ToDouble()
            => AsDouble(_value);

        public override string ToString()
            => AsString(_value);

        public bool FromModbus(ReadOnlySpan<ushort> modbusData)
        {
            var newValue = ReadFromMudbus(modbusData);

            if (newValue.Equals(_value))
                return false;

            _value = newValue;

            return true;
        }

        public bool FromModbus(ReadOnlySpan<bool> data)
            => throw new NotImplementedException();

        public byte[] ToMqtt()
            => MqttStringConverter.ToMqtt(AsString(_value));

        public void ToModbus(double doubleValue, Span<ushort> modbusData)
            => WriteFromMudbus(modbusData, FromDouble(doubleValue));

        public void ToModbus(ReadOnlySpan<byte> mqttData, Span<ushort> modbusData)
            => WriteFromMudbus(modbusData, FromString(MqttStringConverter.FromMqtt(mqttData)));

        public void ToModbus(ReadOnlySpan<byte> mqttData, Span<bool> modbusData)
            => throw new NotImplementedException();
    }
}
