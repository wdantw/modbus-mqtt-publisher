using System.Text;

namespace ModbusMqttPublisher.Server.Services.Types
{
	public class StringPublishedValue : IPublishValue
	{
		public StringPublishedValue(string value)
		{
			Value = value;
		}

		public string Value { get; set; }

		public bool Equals(IPublishValue? other)
		{
			var commpared = other as StringPublishedValue;
			return string.Equals(Value, commpared?.Value ?? string.Empty);
		}

		public byte[] ToMqtt()
		{
			return Encoding.UTF8.GetBytes(Value);
		}

		public void UpdateFrom(IPublishValue value)
		{
			Value = ((StringPublishedValue)value).Value;
		}
	}
}
