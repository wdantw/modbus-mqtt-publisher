using MudbusMqttPublisher.Server.Contracts.Configs;
using MudbusMqttPublisher.Server.Contracts.Settings;

namespace MudbusMqttPublisher.Server.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IConfiguration configuration;
        
        public SettingsService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public PortSettings[] GetSettings()
        {
            var templates = configuration.GetSection("ModbusDeviceTypes").Get<ModbudsDeviceType[]>();
            var converter = new ConfigConverter(templates);

            var ports = configuration.GetSection("ModbusPorts").Get<ModbusPort[]>();

            List<PortSettings> portsList = new();

            foreach(var portCfg in ports)
            {
                if (portCfg.SerialName == null)
                    throw new Exception("SerialName не указан");

                var devices = new List<DeviceSettings>();

                foreach(var devCfg in portCfg.Devices)
                {
                    if (!devCfg.SlaveAddress.HasValue)
                        throw new Exception("Не указан SlaveAddress");

                    var regs = converter.ResolveRegisterList(devCfg.DeviceTypeName, devCfg.Registers, devCfg.Name);

                    devices.Add(new DeviceSettings(
                        devCfg.SlaveAddress.Value,
                        devCfg.MaxRegHole ?? portCfg.MaxRegHole,
                        devCfg.MaxBitHole ?? portCfg.MaxBitHole,
                        devCfg.MaxReadRegisters ?? portCfg.MaxReadRegisters,
                        devCfg.MaxReadBit ?? portCfg.MaxReadBit,
                        regs.ToArray()
                        ));

                }

                portsList.Add(new PortSettings(
                    portCfg.SerialName,
                    portCfg.BaudRate,
                    portCfg.DataBits,
                    portCfg.Parity,
                    portCfg.StopBits,
                    portCfg.MinSleepTimeout,
                    devices.ToArray()
                    ));
            }

            return portsList.ToArray();
        }
    }
}
