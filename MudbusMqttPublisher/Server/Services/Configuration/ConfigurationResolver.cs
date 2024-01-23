using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Options;
using MudbusMqttPublisher.Server.Contracts.Configs;
using MudbusMqttPublisher.Server.Contracts.Settings;
using System.Linq;

namespace MudbusMqttPublisher.Server.Services.Configuration
{
    public interface ITypeResolver
    {
        void Resolve(ModbusDeviceCompleteBase dest, Dictionary<string, ITypeResolver> typeMap);
    }

    public class ConfigurationResolver : IConfigurationResolver
    {
        class TypeResolver : ITypeResolver
        {
            private readonly ModbusDeviceTypeConfig typeConfig;
            private bool isInResolving = false;

            public TypeResolver(ModbusDeviceTypeConfig typeConfig)
            {
                this.typeConfig = typeConfig;
            }

            public void Resolve(ModbusDeviceCompleteBase dest, Dictionary<string, ITypeResolver> typeMap)
            {
                if (isInResolving)
                    throw new Exception("Встретилось рекурсивное определение типа");

                try
                {
                    isInResolving = true;
                    dest.MergeModbusDeviceBase(typeConfig, typeMap);

                }
                finally
                {
                    isInResolving = false;

                }
            }
        }

        private readonly IOptions<ModbusDeviceTypes> deviceTypes;
        private readonly IOptions<ModbusPorts> ports;

        public ConfigurationResolver(IOptions<ModbusDeviceTypes> deviceTypes, IOptions<ModbusPorts> ports)
        {
            this.deviceTypes = deviceTypes;
            this.ports = ports;
        }

        public PortSettings[] ResolveConfigs()
        {
            var typeMap = new Dictionary<string, ITypeResolver>();

            if (deviceTypes.Value != null)
            {
                foreach (var type in deviceTypes.Value)
                {
                    if (type.TypeName == null)
                        throw new Exception("Не указано имя типа для типа устройства");

                    typeMap.Add(type.TypeName, new TypeResolver(type));
                }
            }

            if (ports.Value == null)
                return Array.Empty<PortSettings>();

            var restul = ports.Value
                .Select(p => new ModbusPortComplete().MergePort(p, typeMap))
                .Select(p => p.MapToSettings())
                .ToArray();

            if (restul.SelectMany(p => p.Devices).SelectMany(d => d.Registers.Select(r => r.Name)).GroupBy(r => r).Any(r => r.Take(2).Count() > 1))
                throw new Exception("Имеются неуникальные имена регистров");

            return restul;
        }

    }
}
