namespace ModbusMqttPublisher.Server.Services.Types
{
	public interface IPublishValue : IEquatable<IPublishValue>
	{
		void UpdateFrom(IPublishValue value);
		byte[] ToMqtt();
	}

	public interface IRegisterValue : IPublishValue
	{
		void FromModbus(ArraySegment<ushort> data);
		void ToModbus(ArraySegment<ushort> data);
		void FromModbus(ArraySegment<bool> data);
		void ToModbus(ArraySegment<bool> data);
		void FromMqtt(ArraySegment<byte> data);
		double ToDouble();
		void FromDouble(double value);
	}
}
