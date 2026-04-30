using System.Text;

namespace ModbusMqttPublisher.Server.Services.Values
{
	public static class MqttStringConverter
	{
		public static byte[] ToMqtt(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}

		public static string FromMqtt(ReadOnlySpan<byte> data)
		{
			return Encoding.UTF8.GetString(data);
		}
	}
}
