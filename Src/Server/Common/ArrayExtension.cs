using System.Buffers;

namespace ModbusMqttPublisher.Server.Common
{
	public static class ArrayExtension
	{
		public static ArraySegment<T> GetSegment<T>(this T[] array, int start, int length)
		{
			return new ArraySegment<T>(array, start, length);
		}

		public static ReadOnlySpan<T> ToSpan<T>(this ReadOnlySequence<T> sequence)
			=> sequence.IsSingleSegment ? sequence.FirstSpan : sequence.ToArray();

    }
}
