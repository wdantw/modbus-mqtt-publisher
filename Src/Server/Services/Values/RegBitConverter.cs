using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ModbusMqttPublisher.Server.Services.Values
{
    public class RegBitConverter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDataLE(Span<ushort> destination, ulong value)
        {
            if (!BitConverter.TryWriteBytes(MemoryMarshal.Cast<ushort, byte>(destination), value))
                throw new Exception("Буфер недостаточного размера для записи значения");

            if (!BitConverter.IsLittleEndian)
            {
                destination.Slice(0, sizeof(ulong) / sizeof(ushort)).Reverse();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDataBE(Span<ushort> destination, ulong value)
        {
            if (!BitConverter.TryWriteBytes(MemoryMarshal.Cast<ushort, byte>(destination), value))
                throw new Exception("Буфер недостаточного размера для записи значения");

            if (BitConverter.IsLittleEndian)
            {
                destination.Slice(0, sizeof(ulong) / sizeof(ushort)).Reverse();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDataLE(Span<ushort> destination, long value)
        {
            if (!BitConverter.TryWriteBytes(MemoryMarshal.Cast<ushort, byte>(destination), value))
                throw new Exception("Буфер недостаточного размера для записи значения");

            if (!BitConverter.IsLittleEndian)
            {
                destination.Slice(0, sizeof(long) / sizeof(ushort)).Reverse();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDataBE(Span<ushort> destination, long value)
        {
            if (!BitConverter.TryWriteBytes(MemoryMarshal.Cast<ushort, byte>(destination), value))
                throw new Exception("Буфер недостаточного размера для записи значения");

            if (BitConverter.IsLittleEndian)
            {
                destination.Slice(0, sizeof(long) / sizeof(ushort)).Reverse();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDataLE(Span<ushort> destination, uint value)
        {
            if (!BitConverter.TryWriteBytes(MemoryMarshal.Cast<ushort, byte>(destination), value))
                throw new Exception("Буфер недостаточного размера для записи значения");

            if (!BitConverter.IsLittleEndian)
            {
                destination.Slice(0, sizeof(uint) / sizeof(ushort)).Reverse();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDataBE(Span<ushort> destination, uint value)
        {
            if (!BitConverter.TryWriteBytes(MemoryMarshal.Cast<ushort, byte>(destination), value))
                throw new Exception("Буфер недостаточного размера для записи значения");

            if (BitConverter.IsLittleEndian)
            {
                destination.Slice(0, sizeof(uint) / sizeof(ushort)).Reverse();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDataLE(Span<ushort> destination, int value)
        {
            if (!BitConverter.TryWriteBytes(MemoryMarshal.Cast<ushort, byte>(destination), value))
                throw new Exception("Буфер недостаточного размера для записи значения");

            if (!BitConverter.IsLittleEndian)
            {
                destination.Slice(0, sizeof(int) / sizeof(ushort)).Reverse();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDataBE(Span<ushort> destination, int value)
        {
            if (!BitConverter.TryWriteBytes(MemoryMarshal.Cast<ushort, byte>(destination), value))
                throw new Exception("Буфер недостаточного размера для записи значения");

            if (BitConverter.IsLittleEndian)
            {
                destination.Slice(0, sizeof(int) / sizeof(ushort)).Reverse();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteData(Span<ushort> destination, ushort value)
        {
            if (destination.Length < 1)
                throw new Exception("Буфер недостаточного размера для записи значения");

            destination[0] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteData(Span<ushort> destination, short value)
        {
            if (destination.Length < 1)
                throw new Exception("Буфер недостаточного размера для записи значения");

            Unsafe.As<ushort, short>(ref destination[0]) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteData(Span<ushort> destination, string value)
        {
            if (destination.Length * 2 < value.Length)
                throw new Exception("Буфер недостаточного размера для записи значения");

            var bytes = MemoryMarshal.Cast<ushort, byte>(destination);

            Encoding.ASCII.GetBytes(value, bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReverseWords(uint value) => BitOperations.RotateRight(value, 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReverseWords(ulong value) => ((ulong)ReverseWords((uint)value) << 32) + ReverseWords((uint)(value >> 32));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReverseWords(int value) => (int)ReverseWords((uint)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReverseWords(long value) => (long)ReverseWords((ulong)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadDataUInt64LE(ReadOnlySpan<ushort> source)
        {
            var value = BitConverter.ToUInt64(MemoryMarshal.Cast<ushort, byte>(source));

            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseWords(value);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadDataUInt64BE(ReadOnlySpan<ushort> source)
        {
            var value = BitConverter.ToUInt64(MemoryMarshal.Cast<ushort, byte>(source));

            if (BitConverter.IsLittleEndian)
            {
                value = ReverseWords(value);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadDataInt64LE(ReadOnlySpan<ushort> source)
        {
            var value = BitConverter.ToInt64(MemoryMarshal.Cast<ushort, byte>(source));

            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseWords(value);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadDataInt64BE(ReadOnlySpan<ushort> source)
        {
            var value = BitConverter.ToInt64(MemoryMarshal.Cast<ushort, byte>(source));

            if (BitConverter.IsLittleEndian)
            {
                value = ReverseWords(value);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadDataUInt32LE(ReadOnlySpan<ushort> source)
        {
            var value = BitConverter.ToUInt32(MemoryMarshal.Cast<ushort, byte>(source));

            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseWords(value);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadDataUInt32BE(ReadOnlySpan<ushort> source)
        {
            var value = BitConverter.ToUInt32(MemoryMarshal.Cast<ushort, byte>(source));

            if (BitConverter.IsLittleEndian)
            {
                value = ReverseWords(value);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadDataInt32LE(ReadOnlySpan<ushort> source)
        {
            var value = BitConverter.ToInt32(MemoryMarshal.Cast<ushort, byte>(source));

            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseWords(value);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadDataInt32BE(ReadOnlySpan<ushort> source)
        {
            var value = BitConverter.ToInt32(MemoryMarshal.Cast<ushort, byte>(source));

            if (BitConverter.IsLittleEndian)
            {
                value = ReverseWords(value);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadDataUInt16(ReadOnlySpan<ushort> source)
        {
            return source[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadDataInt16(ReadOnlySpan<ushort> source)
        {
            return MemoryMarshal.Cast<ushort, short>(source)[0];
        }
    }
}
