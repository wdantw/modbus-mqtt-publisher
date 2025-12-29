namespace ModbusMqttPublisher.Server.Services.Modbus.Utils
{
    public class ModbusReadBuffer : IChannelDataReader
    {
        private readonly IModbusChannel _channel;
        private readonly byte[] _buffer;
        private int _position = 0;

        public byte[] Buffer => _buffer;

        public int Position => _position;

        public ModbusReadBuffer(byte[] buffer, IModbusChannel channel)
        {
            _buffer = buffer;
            _channel = channel;
        }

        public void Reset()
        {
            _position = 0;
        }

        public Span<byte> Whole()
        {
            return _buffer.AsSpan().Slice(0, _position);
        }

        public ReadOnlySpan<byte> Read(int size)
        {
            _channel.Read(_buffer, _position, size);
            var result = _buffer.AsSpan().Slice(_position, size);
            _position += size;
            return result;
        }
    }
}
