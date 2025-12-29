namespace ModbusMqttPublisher.Server.Services.Modbus.Utils
{
    public interface IChannelDataWriter
    {
        Span<byte> Alloc(int size);
    }
}
