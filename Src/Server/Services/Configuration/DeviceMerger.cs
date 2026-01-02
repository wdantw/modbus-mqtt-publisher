using ModbusMqttPublisher.Server.Contracts.Configs;

namespace ModbusMqttPublisher.Server.Services.Configuration
{
	public static class DeviceMerger
    {
        public static T MergeReadParameters<T>(this T dest, ReadParameters overrides)
            where T : ReadParameters
        {
            dest.MaxRegHole = overrides.MaxRegHole ?? dest.MaxRegHole;
            dest.MaxBitHole = overrides.MaxBitHole ?? dest.MaxBitHole;
            dest.MaxReadRegisters = overrides.MaxReadRegisters ?? dest.MaxReadRegisters;
            dest.MaxReadBit = overrides.MaxReadBit ?? dest.MaxReadBit;
			return dest;
        }

        public static T ApplyRegisterModifires<T>(this T dest, IEnumerable<ModbusRegisterModifier> modifiers)
            where T : ModbusDeviceCompleteBase
		{
            dest.Registers.ApplyRegistersModifiers(modifiers);
            return dest;
        }

		public static ModbusDeviceCompleteBase MergeModbusDeviceBase(
            this ModbusDeviceCompleteBase dest,
            ModbusDeviceConfigBase overrides,
            Dictionary<string, ITypeResolver> typeMap,
            IEnumerable<ModbusRegisterModifier> globalModifiers)
        {
            if (overrides.ParentTypeName != null)
            {
                if (!typeMap.TryGetValue(overrides.ParentTypeName, out var parentResolver))
                    throw new Exception($"Не найден родительский тип устройства {overrides.ParentTypeName}");

                parentResolver.Resolve(dest, typeMap, globalModifiers);
            }

            dest.MergeReadParameters(overrides);
            dest.Name = overrides.Name ?? dest.Name;
            dest.ParentTypeName = overrides.ParentTypeName ?? dest.ParentTypeName;
            dest.Registers.MergeRegisters(overrides.Registers, overrides.Modifiers, globalModifiers);

            return dest;
        }

        public static ModbusDeviceComplete MergeDevice(
            this ModbusDeviceComplete dest,
            ModbusDeviceConfig overrides,
            Dictionary<string, ITypeResolver> typeMap,
            IEnumerable<ModbusRegisterModifier> globalModifiers)
        {
            dest.MergeModbusDeviceBase(overrides, typeMap, globalModifiers);
            dest.SlaveAddress = overrides.SlaveAddress;
            return dest;
        }

        public static ModbusPortComplete MergePort(
            this ModbusPortComplete dest,
            ModbusPortConfig overrides,
            Dictionary<string, ITypeResolver> typeMap,
            IEnumerable<ModbusRegisterModifier> globalModifiers)
        {
            dest.MergeReadParameters(overrides);
            dest.Name = overrides.Name ?? dest.Name;
            dest.SerialName = overrides.SerialName ?? dest.SerialName;
            dest.BaudRate = overrides.BaudRate ?? dest.BaudRate;
            dest.DataBits = overrides.DataBits ?? dest.DataBits;
            dest.Parity = overrides.Parity ?? dest.Parity;
			dest.StopBits = overrides.StopBits ?? dest.StopBits;
            dest.MinSleepTimeout = overrides.MinSleepTimeout ?? dest.MinSleepTimeout;
            dest.RetryCount = overrides.RetryCount ?? dest.RetryCount;
            dest.ErrorSleepTimeout = overrides.ErrorSleepTimeout ?? dest.ErrorSleepTimeout;
            dest.ReadTimeout = overrides.ReadTimeout ?? dest.ReadTimeout;
            dest.WriteTimeout = overrides.WriteTimeout ?? dest.WriteTimeout;
            dest.AllowWbEvents = overrides.AllowWbEvents ?? dest.AllowWbEvents;

            dest.Devices = overrides.Devices.Select(d =>
                new ModbusDeviceComplete()
                    .MergeReadParameters(dest)
                    .MergeDevice(d, typeMap, globalModifiers)
					.ApplyRegisterModifires(overrides.Modifiers)
			).ToArray();
            return dest;
        }


    }
}
