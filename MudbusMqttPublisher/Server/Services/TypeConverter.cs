using MudbusMqttPublisher.Server.Contracts;
using System.Text;

namespace MudbusMqttPublisher.Server.Services
{
    public static class TypeConverter
    {
        static void AssertLength(ReadOnlySpan<ushort> arr, int length)
        {
            if (arr.Length < length)
                throw new ArgumentOutOfRangeException("Переданных данных не достаточно для преобразования значения");
        }

        public static object ConvertFromDevice(ushort[] data, RegisterFormat format, int? length)
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

        public static ushort[] ConvertToDevice(object value, RegisterFormat format, int? length)
        {
            switch (format)
            {
                case RegisterFormat.Default:
                    return new ushort[] { Convert.ToUInt16(value) };

                case RegisterFormat.Uint32:
                    return Convert.ToUInt32(value).SplitUint().ToArray();

                case RegisterFormat.Uint64:
                    return Convert.ToUInt64(value).SplitUlong().SelectMany(SplitUint).ToArray();

                case RegisterFormat.Int16:
                    return new ushort[] { Convert.ToInt16(value).MakeUnsigned() };

                case RegisterFormat.Int32:
                    return Convert.ToInt32(value).MakeUnsigned().SplitUint().ToArray();

                case RegisterFormat.Int64:
                    return Convert.ToInt64(value).MakeUnsigned().SplitUlong().SelectMany(SplitUint).ToArray();

                case RegisterFormat.String:
                    {
                        if (!length.HasValue)
                            throw new ArgumentNullException(nameof(length));
                        var result = new ushort[length.Value];
                        var str = Convert.ToString(value);
                        if (str != null)
                        {
                            var bytes = Encoding.ASCII.GetBytes(str);
                            for(var i = 0; i < bytes.Length; i += 2)
                            {
                                var b1 = bytes[0];
                                byte b2 = 0;
                                if (i + 1 < bytes.Length)
                                    b2 = bytes[i + 1];

                                result[i % 2] = (ushort)((b1 << 8) + b2);
                            }
                        }
                        return result;
                    }

                default:
                    throw new ArgumentException("Неизвестное значение формата регистра");
            }
        }

        static uint ToUint32(ushort v1, ushort v2) => ((uint)v1 << 16) + v2;
        static ulong ToUint64(ushort v1, ushort v2, ushort v3, ushort v4) => ((ulong)ToUint32(v1, v2) << 32) + ToUint32(v3, v4);
        static short MakeSigned(ushort src) => unchecked((short)src);
        static int MakeSigned(uint src) => unchecked((short)src);
        static long MakeSigned(ulong src) => unchecked((short)src);
        static ushort MakeUnsigned(this short src) => unchecked((ushort)src);
        static uint MakeUnsigned(this int src) => unchecked((ushort)src);
        static ulong MakeUnsigned(this long src) => unchecked((ushort)src);

        static IEnumerable<byte> SplitUshort(this ushort value)
        {
            yield return (byte)((value & 0xFF00) >> 8);
            yield return (byte)(value & 0xFF);
        }

        static IEnumerable<ushort> SplitUint(this uint value)
        {
            yield return (ushort)((value & 0xFFFF0000) >> 16);
            yield return (ushort)(value & 0xFFFF);
        }

        static IEnumerable<uint> SplitUlong(this ulong value)
        {
            yield return (uint)((value & 0xFFFFFFFF00000000) >> 32);
            yield return (uint)(value & 0xFFFFFFFF);
        }

    }
}
