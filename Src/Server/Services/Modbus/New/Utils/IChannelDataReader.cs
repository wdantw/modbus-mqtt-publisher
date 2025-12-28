namespace ModbusMqttPublisher.Server.Services.Modbus.New.Utils
{
    public interface IChannelDataReader
    {
        ReadOnlySpan<byte> Read(int size);
    }
}
