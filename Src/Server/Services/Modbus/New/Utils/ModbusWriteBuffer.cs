namespace ModbusMqttPublisher.Server.Services.Modbus.New.Utils
{
    public class ModbusWriteBuffer : IChannelDataWriter
    {
        private readonly byte[] _buffer;
        private int _position = 0;

        public byte[] Buffer => _buffer;

        public int Position => _position;

        public ModbusWriteBuffer(byte[] buffer)
        {
            _buffer = buffer;
        }

        public void Reset()
        {
            _position = 0;
        }

        public Span<byte> Alloc(int size)
        {
            var result = _buffer.AsSpan().Slice(_position, size);
            _position += size;
            return result;
        }

        public Span<byte> Whole()
        {
            return _buffer.AsSpan().Slice(0, _position);
        }
    }
}
