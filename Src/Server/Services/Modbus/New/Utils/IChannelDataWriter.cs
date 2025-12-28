namespace ModbusMqttPublisher.Server.Services.Modbus.New.Utils
{
    public interface IChannelDataWriter
    {
        Span<byte> Alloc(int size);
    }
}
