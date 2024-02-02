namespace MudbusMqttPublisher.Server.Common
{
	public static class ArrayExtension
	{
		public static ArraySegment<T> GetSegment<T>(this T[] array, int start, int length)
		{
			return new ArraySegment<T>(array, start, length);
		}
	}
}
