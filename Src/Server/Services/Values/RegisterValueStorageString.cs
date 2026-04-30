using System.Runtime.InteropServices;
using System.Text;

namespace ModbusMqttPublisher.Server.Services.Values
{
	public class RegisterValueStorageString : IRegisterValueStorageWithInConverter
	{
		string? _value = null;

		public bool FromModbus(ReadOnlySpan<ushort> data)
		{
			var bytes = MemoryMarshal.Cast<ushort, byte>(data);
			var nonZeroBytes = new byte[bytes.Length];
			var newLength = 0;
			for (var i = 0; i < bytes.Length; i++)
			{
				var byteValue = bytes[i];
				if (byteValue != 0)
					nonZeroBytes[newLength++] = bytes[i];
			}

			var newValue = string.Empty;

			if (newLength > 0)
			{
				newValue = Encoding.ASCII.GetString(nonZeroBytes, 0, newLength);
			}

			if (_value != null && string.Equals(_value, newValue))
				return false;

			_value = newValue;

			return true;
		}

		public bool FromModbus(ReadOnlySpan<bool> data) => throw new NotImplementedException();

		public byte[] ToMqtt()
		{
			if (_value != null && _value.Length > 0)
				return MqttStringConverter.ToMqtt(_value);
			else
				return Array.Empty<byte>();
		}

		public override string ToString() => _value ?? "<null>";

		public void ToModbus(ReadOnlySpan<byte> mqttData, Span<ushort> modbusData)
		{
			var strData = MqttStringConverter.FromMqtt(mqttData);
			var modbusBytes = MemoryMarshal.Cast<ushort, byte>(modbusData);

			if (strData.Length > modbusBytes.Length)
				strData = strData.Substring(0, modbusBytes.Length);

            Encoding.ASCII.GetBytes(strData, modbusBytes);
			for (int i = strData.Length; i < modbusBytes.Length; i++)
				modbusBytes[i] = 0;
		}

		public void ToModbus(ReadOnlySpan<byte> mqttData, Span<bool> modbusData) => throw new NotImplementedException();

	}
}
