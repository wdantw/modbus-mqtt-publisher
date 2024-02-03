using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModbusMqttPublisher.Server.Services.Values
{
	public static class TypeUtils
    {

        public delegate bool BitConverterWriteFunc<T>(Span<byte> bits, T value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertToModbus<T>(T incomeValue, Span<ushort> modbusData, BitConverterWriteFunc<T> writeFunc)
        {
            var modbusBytes = MemoryMarshal.Cast<ushort, byte>(modbusData);
            if (!writeFunc(modbusBytes, incomeValue))
                throw new Exception("Буфер недостаточного размера для записи значения");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertToModbus<T>(ReadOnlySpan<byte> mqttData, Span<ushort> modbusData, Func<string, T> parseFunc, BitConverterWriteFunc<T> writeFunc)
        {
            var strData = MqttStringConverter.FromMqtt(mqttData);
            var incomeValue = parseFunc(strData);
            ConvertToModbus(incomeValue, modbusData, writeFunc);
        }

        public delegate T BitConverterFunc<T>(ReadOnlySpan<byte> bits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FromModbus<T>(ref T value, ReadOnlySpan<ushort> data, BitConverterFunc<T> converter)
            where T : struct, IEquatable<T>
        {
            var bytes = MemoryMarshal.Cast<ushort, byte>(data);
            var newValue = converter(bytes);
            return FromModbus(ref value, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FromModbus<T>(ref T value, T newValue)
            where T : struct, IEquatable<T>
        {
            if (newValue.Equals(value))
                return false;

            value = newValue;

            return true;
        }
    }
}
