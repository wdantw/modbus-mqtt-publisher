namespace ModbusMqttPublisher.Server.Services.Values
{
	public class PublishValueStorageString : IPublishValueSorage
	{
		private readonly string _value;

		public PublishValueStorageString(string value)
		{
			_value = value;
		}

		public byte[] ToMqtt() => MqttStringConverter.ToMqtt(_value);

		public override string ToString() => _value;
	}
}
