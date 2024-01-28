namespace MudbusMqttPublisher.Server.Services.Types
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

		public static ArraySegment<T> GetSegment<T>(this T[] array, int start, int length)
		{
			return new ArraySegment<T>(array, start, length);
		}
	}
}
