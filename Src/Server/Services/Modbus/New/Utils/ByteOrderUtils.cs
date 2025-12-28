using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace ModbusMqttPublisher.Server.Services.Modbus.New.Utils
{
    public static class ByteOrderUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBE(Span<byte> destination, ushort value)
        {
            if (BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);

            if (!BitConverter.TryWriteBytes(destination, value))
                throw new IndexOutOfRangeException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLE(Span<byte> destination, ushort value)
        {
            if (!BitConverter.IsLittleEndian)
                value = BinaryPrimitives.ReverseEndianness(value);

            if (!BitConverter.TryWriteBytes(destination, value))
                throw new IndexOutOfRangeException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16BE(ReadOnlySpan<byte> source)
        {
            var result = BitConverter.ToUInt16(source);

            if (BitConverter.IsLittleEndian)
                result = BinaryPrimitives.ReverseEndianness(result);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16LE(ReadOnlySpan<byte> source)
        {
            var result = BitConverter.ToUInt16(source);

            if (!BitConverter.IsLittleEndian)
                result = BinaryPrimitives.ReverseEndianness(result);

            return result;
        }

    }
}
