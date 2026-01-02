using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.Handlers
{
    public interface IModbusRequestHandler<TResult>
    {
        ModbusFunctionCode RequestFunctionCode { get; }

        byte RequestSlaveAddress { get; }

        bool SkeepStartWbArbitration { get; }

        TResult ReadAnswer(ModbusHeader header, IChannelDataReader reader);

        void WriteRequest(IChannelDataWriter writer);

        string GetRequestInformation();
    }
}