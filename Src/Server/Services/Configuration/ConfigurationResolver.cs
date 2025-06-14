using Microsoft.Extensions.Options;
using ModbusMqttPublisher.Server.Contracts.Configs;
using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services.Configuration
{
    public interface ITypeResolver
    {
        void Resolve(ModbusDeviceCompleteBase dest, Dictionary<string, ITypeResolver> typeMap, IEnumerable<ModbusRegisterModifier> globalModifiers);
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

            public void Resolve(ModbusDeviceCompleteBase dest, Dictionary<string, ITypeResolver> typeMap, IEnumerable<ModbusRegisterModifier> globalModifiers)
            {
                if (isInResolving)
                    throw new Exception("Встретилось рекурсивное определение типа");

                try
                {
                    isInResolving = true;
                    dest.MergeModbusDeviceBase(typeConfig, typeMap, globalModifiers);

                }
                finally
                {
                    isInResolving = false;

                }
            }
        }

        private readonly IOptions<ModbusDeviceTypes> deviceTypes;
		private readonly IOptions<ModbusPorts> ports;
		private readonly IOptions<ModbusModifiers> globalModifires;

		public ConfigurationResolver(IOptions<ModbusDeviceTypes> deviceTypes, IOptions<ModbusPorts> ports, IOptions<ModbusModifiers> globalModifires)
		{
			this.deviceTypes = deviceTypes;
			this.ports = ports;
			this.globalModifires = globalModifires;
		}

		public ReadPort[] ResolveConfigs()
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
                return Array.Empty<ReadPort>();

            var modifires = globalModifires.Value?.AsEnumerable() ?? Array.Empty<ModbusRegisterModifier>();

            var restul = ports.Value
                .Select(p => new ModbusPortComplete().MergePort(p, typeMap, modifires))
                .Select(p => new ReadPort(p))
                .ToArray();

			var dublicateNames = restul
                .SelectMany(x => x.Devices)
                .SelectMany(x => x.Groups)
                .SelectMany(d => d.Registers.Select(r => r.Name))
                .GroupBy(rname => rname)
                .Where(r => r.Skip(1).Any())
                .Select(g => g.Key)
                .ToArray();

			if (dublicateNames.Length > 0)
                throw new Exception($"Имеются неуникальные имена регистров: {string.Join(", ", dublicateNames)}");

            return restul;
        }

    }
}
