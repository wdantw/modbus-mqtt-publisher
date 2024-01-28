using MudbusMqttPublisher.Server.Contracts.Configs;

namespace MudbusMqttPublisher.Server.Services.Configuration
{
	public static class DeviceMerger
    {
        public static T MergeReadParameters<T>(this T dest, ReadParameters overrides)
            where T : ReadParameters
        {
            dest.MinSleepTimeout = overrides.MinSleepTimeout ?? dest.MinSleepTimeout;
            dest.AllowWbEvents = overrides.AllowWbEvents ?? dest.AllowWbEvents;
            dest.MaxRegHole = overrides.MaxRegHole ?? dest.MaxRegHole;
            dest.MaxBitHole = overrides.MaxBitHole ?? dest.MaxBitHole;
            dest.MaxReadRegisters = overrides.MaxReadRegisters ?? dest.MaxReadRegisters;
            dest.MaxReadBit = overrides.MaxReadBit ?? dest.MaxReadBit;
			dest.ReadTimeout = overrides.ReadTimeout ?? dest.ReadTimeout;
			dest.WriteTimeout = overrides.WriteTimeout ?? dest.WriteTimeout;
			dest.WriteRetryCount = overrides.WriteRetryCount ?? dest.WriteRetryCount;
			dest.ReadRetryCount = overrides.ReadRetryCount ?? dest.ReadRetryCount;
			dest.ErrorSleepTimeout = overrides.ErrorSleepTimeout ?? dest.ErrorSleepTimeout;
			return dest;
        }

        public static T ApplyRegisterModifires<T>(this T dest, IEnumerable<ModbusRegisterModifier> modifiers)
            where T : ModbusDeviceCompleteBase
		{
            dest.Registers.ApplyRegistersModifiers(modifiers);
            return dest;
        }

		public static ModbusDeviceCompleteBase MergeModbusDeviceBase(this ModbusDeviceCompleteBase dest, ModbusDeviceConfigBase overrides, Dictionary<string, ITypeResolver> typeMap)
        {
            if (overrides.ParentTypeName != null)
            {
                if (!typeMap.TryGetValue(overrides.ParentTypeName, out var parentResolver))
                    throw new Exception($"Не найден родительский тип устройства {overrides.ParentTypeName}");

                parentResolver.Resolve(dest, typeMap);
            }

            dest.MergeReadParameters(overrides);
            dest.Name = overrides.Name ?? dest.Name;
            dest.ParentTypeName = overrides.ParentTypeName ?? dest.ParentTypeName;
            dest.Registers.MergeRegisters(overrides.Registers, overrides.Modifiers);

            return dest;
        }

        public static ModbusDeviceComplete MergeDevice(this ModbusDeviceComplete dest, ModbusDeviceConfig overrides, Dictionary<string, ITypeResolver> typeMap)
        {
            dest.MergeModbusDeviceBase(overrides, typeMap);
            dest.SlaveAddress = overrides.SlaveAddress;
            return dest;
        }

        public static ModbusPortComplete MergePort(this ModbusPortComplete dest, ModbusPortConfig overrides, Dictionary<string, ITypeResolver> typeMap, IEnumerable<ModbusRegisterModifier> globalModifiers)
        {
            dest.MergeReadParameters(overrides);
            dest.Name = overrides.Name ?? dest.Name;
            dest.SerialName = overrides.SerialName ?? dest.SerialName;
            dest.BaudRate = overrides.BaudRate ?? dest.BaudRate;
            dest.DataBits = overrides.DataBits ?? dest.DataBits;
            dest.Parity = overrides.Parity ?? dest.Parity;
			dest.StopBits = overrides.StopBits ?? dest.StopBits;
			
			dest.Devices = overrides.Devices.Select(d =>
                new ModbusDeviceComplete()
                    .MergeReadParameters(dest)
                    .MergeDevice(d, typeMap)
                    .ApplyRegisterModifires(overrides.Modifiers)
                    .ApplyRegisterModifires(globalModifiers)
			).ToArray();
            return dest;
        }


    }
}
