namespace MudbusMqttPublisher.Server.Services.Types
{
	public interface IRegisterValue : IEquatable<IRegisterValue>
	{
		void FromModbus(ArraySegment<ushort> data);
		void ToModbus(ArraySegment<ushort> data);
		void FromModbus(ArraySegment<bool> data);
		void ToModbus(ArraySegment<bool> data);
		void FromMqtt(ArraySegment<byte> data);
		byte[] ToMqtt();
		double ToDouble();
		void FromDouble(double value);
		void UpdateFrom(IRegisterValue value);
	}
}
