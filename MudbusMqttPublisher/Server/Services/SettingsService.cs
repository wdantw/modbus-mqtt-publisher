using MudbusMqttPublisher.Server.Common;
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

                var nonUniqueReg = devices.SelectMany(d => d.Registers.Select(r => r.Name)).GroupBy(r => r).FirstOrDefault(r => r.Take(2).Count() > 1);
                if (nonUniqueReg != null)
                    throw new Exception($"Имеются неуникальные имена '{nonUniqueReg.Key}' регистров в рамках одного порта '{portCfg.SerialName}'");

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

            if (portsList.SelectMany(p => p.Devices).SelectMany(d => d.Registers.Select(r => r.Name)).GroupBy(r => r).Any(r => r.Take(2).Count() > 1))
                throw new Exception("Имеются неуникальные имена регистров в рамках всей службы");

            return portsList.ToArray();
        }
    }
}
