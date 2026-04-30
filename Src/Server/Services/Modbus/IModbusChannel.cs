namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public interface IModbusChannel
    {
        void Read(byte[] buffer, int offset, int count);

        void Write(byte[] buffer, int offset, int count);

        void DiscardInBuffer();
    }
}
