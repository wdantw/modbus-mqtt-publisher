namespace ModbusMqttPublisher.Server.Services.Types
{
	public static class TypeUtils
	{
		public static byte[] ToBytes(ReadOnlySpan<ushort> data)
		{
			var result = new byte[data.Length * 2];
			for (int i = 0; i < data.Length; i++)
			{
				if (!BitConverter.TryWriteBytes(result.AsSpan(i * 2, 2), data[i]))
					throw new InvalidOperationException("TryWriteBytes вернул false");
			}
			return result;
		}

		public static void FromBytes(ReadOnlySpan<byte> source, Span<ushort> destination)
		{
			for (int i = 0; i < destination.Length; i++)
			{
				destination[i] = BitConverter.ToUInt16(source.Slice(i * 2, 2));
			}
		}

		public static void FromDouble(ref bool dest, double source) => dest = Convert.ToBoolean(source);
		public static void FromDouble(ref ushort dest, double source) => dest = (ushort)Math.Round(source);
		public static void FromDouble(ref uint dest, double source) => dest = (uint)Math.Round(source);
		public static void FromDouble(ref ulong dest, double source) => dest = (ulong)Math.Round(source);
		public static void FromDouble(ref short dest, double source) => dest = (short)Math.Round(source);
		public static void FromDouble(ref int dest, double source) => dest = (int)Math.Round(source);
		public static void FromDouble(ref long dest, double source) => dest = (long)Math.Round(source);
	}
}
