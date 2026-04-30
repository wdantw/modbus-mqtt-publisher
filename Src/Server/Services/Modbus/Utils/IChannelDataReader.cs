namespace ModbusMqttPublisher.Server.Services.Modbus.Utils
{
    public interface IChannelDataReader
    {
        ReadOnlySpan<byte> Read(int size);
    }
}
