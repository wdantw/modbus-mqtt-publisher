using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public class ModbusSerialPortFactory : IModbusSerialPortFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ModbusSerialPortFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IModbusSerialPort Create(ReadPort settings)
        {
            return ActivatorUtilities.CreateInstance<ModbusSerialPort>(_serviceProvider, settings);
        }
    }
}
