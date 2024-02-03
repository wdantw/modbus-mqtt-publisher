namespace ModbusMqttPublisher.Server.Services.Values
{
	public class PublishValueStorageDouble : IPublishValueSorage
	{
		private readonly double _value;

		public PublishValueStorageDouble(double value)
		{
			_value = value;
		}

		public byte[] ToMqtt() => MqttStringConverter.ToMqtt(_value.ToString(NumberFormatHelper.DefaultNumberFormat));

		public override string ToString() => _value.ToString(NumberFormatHelper.DefaultNumberFormat);
	}

}