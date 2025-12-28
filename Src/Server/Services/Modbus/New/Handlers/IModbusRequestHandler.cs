using ModbusMqttPublisher.Server.Services.Modbus.New.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.New.Utils;

namespace ModbusMqttPublisher.Server.Services.Modbus.New.Handlers
{
    public interface IModbusRequestHandler<TResult>
    {
        ModbusFunctionCode RequestFunctionCode { get; }

        byte RequestSlaveAddress { get; }

        TResult ReadAnswer(ModbusHeader header, IChannelDataReader reader);

        void WriteRequest(IChannelDataWriter writer);
    }
}