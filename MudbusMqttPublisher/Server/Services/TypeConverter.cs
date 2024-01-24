using MudbusMqttPublisher.Server.Contracts;
using System.Text;

namespace MudbusMqttPublisher.Server.Services
{
    public static class TypeConverter
    {
        public static object Convert(ushort[] data, RegisterFormat format, int? length)
        {
            switch (format)
            {
                case RegisterFormat.Default:
                    AssertLength(data, 1);
                    return data[0];

                case RegisterFormat.Uint32:
                    AssertLength(data, 2);
                    return ToUint32(data[0], data[1]);

                case RegisterFormat.Uint64:
                    AssertLength(data, 4);
                    return ToUint64(data[0], data[1], data[2], data[3]);

                case RegisterFormat.Int16:
                    AssertLength(data, 1);
                    return MakeSigned(data[0]);

                case RegisterFormat.Int32:
                    AssertLength(data, 2);
                    return MakeSigned(ToUint32(data[0], data[1]));

                case RegisterFormat.Int64:
                    AssertLength(data, 4);
                    return MakeSigned(ToUint64(data[0], data[1], data[2], data[3]));

                case RegisterFormat.String:
                    if (!length.HasValue) throw new ArgumentNullException(nameof(length));
                    AssertLength(data, length.Value);
                    return Encoding.ASCII.GetString(data.Take(length.Value).SelectMany(SplitUshort).ToArray());

                default:
                    throw new ArgumentException("Неизвестное значение формата регистра");
            }
        }

        static void AssertLength(ReadOnlySpan<ushort> arr, int length)
        {
            if (arr.Length < length)
                throw new ArgumentOutOfRangeException("Переданных данных не достаточно для преобразования значения");
        }

        static uint ToUint32(ushort v1, ushort v2) => ((uint)v1 << 16) + v2;
        static ulong ToUint64(ushort v1, ushort v2, ushort v3, ushort v4) => ((ulong)ToUint32(v1, v2) << 32) + ToUint32(v3, v4);
        static short MakeSigned(ushort src) => unchecked((short)src);
        static int MakeSigned(uint src) => unchecked((short)src);
        static long MakeSigned(ulong src) => unchecked((short)src);

        static IEnumerable<byte> SplitUshort(this ushort value)
        {
            yield return (byte)((value & 0xFF00) >> 8);
            yield return (byte)(value & 0xFF);
        }
    }
}
