using MudbusMqttPublisher.Server.Contracts;
using MudbusMqttPublisher.Server.Contracts.Configs;
using MudbusMqttPublisher.Server.Contracts.Settings;
using System.Text.RegularExpressions;

namespace MudbusMqttPublisher.Server.Services
{
    public class ConfigConverter
    {

        private class RegisterList
        {
            private List<RegisterSettings> registers = new();

            public IReadOnlyCollection<RegisterSettings> Registers => registers;

            public RegisterList()
            {

            }

            public RegisterList(RegisterList source)
            {
                foreach(var reg in source.registers)
                {
                    registers.Add(new RegisterSettings(reg));
                }
            }

            private void AddSingle(ModbusDeviceRegister config, ushort number, string name, ConfigRegisterType regType)
            {
                if (registers.Any(r => r.Name == name))
                    throw new Exception($"Регистр с именем {name} уже есть в этом устройстве");

                var newReg = new RegisterSettings(name, number, regType.GetRegisterType(), regType.GetRegisterFormat(), config.ReadPeriod ?? TimeSpan.MaxValue, config.Length,
                    config.WbEvents ?? false, config.Scale);

                if (!registers.All(r => newReg.EndRegisterNumber <= r.Number || newReg.Number >= r.EndRegisterNumber))
                    throw new Exception($"Регистр с номером из диапазона {number}-{newReg.EndRegisterNumber-1} уже есть в этом устройстве");

                registers.Add(newReg);
            }

            private void PatchSingle(ModbusDeviceRegister config, RegisterSettings register)
            {
                if (config.Number.HasValue) throw new Exception("Нельзя переопределять значение Number");
                if (config.NumberStart.HasValue) throw new Exception("Нельзя переопределять значение NumberStart");
                if (config.NumberCount.HasValue) throw new Exception("Нельзя переопределять значение NumberCount");
                if (config.ColCount.HasValue) throw new Exception("Нельзя переопределять значение ColCount");
                if (config.ColLength.HasValue) throw new Exception("Нельзя переопределять значение ColLength");
                if (config.RowCount.HasValue) throw new Exception("Нельзя переопределять значение RowCount");
                if (config.Length.HasValue) throw new Exception("Нельзя переопределять значение Length");
                if (config.RegType.HasValue) throw new Exception("Нельзя переопределять значение RegType");

                if (config.Scale.HasValue)
                    register.UpdateScale(config.Scale.Value);

                if (config.WbEvents.HasValue)
                    register.UpdateWbEvents(config.WbEvents.Value);

                if (config.ReadPeriod.HasValue)
                    register.UpdatePeriod(config.ReadPeriod.Value);
            }

            public void AddRegister(ModbusDeviceRegister config)
            {
                if (config.Names != null)
                {
                    if (config.Name != null) throw new Exception("Нельзя одновременно указывать Names и Name");

                    var regex = new Regex("^" + config.Names + "$");

                    foreach (var reg in registers)
                    {
                        if (regex.IsMatch(reg.Name))
                            PatchSingle(config, reg);
                    }

                    return;
                }

                if (config.Name == null) throw new Exception("Не указан Name");

                var regWithName = registers.FirstOrDefault(r => r.Name == config.Name);

                if (regWithName != null)
                {
                    PatchSingle(config, regWithName);
                    return;
                }

                if (!config.RegType.HasValue) throw new Exception("Не указан RegType");

                var formatSize = config.RegType.Value.GetRegisterFormat().SizeInRegisters();

                if (config.NumberStart.HasValue)
                {
                    if (config.Number.HasValue) throw new Exception("Нельзя одновременно указывать и NumberStart и Number");
                    if (config.Names != null) throw new Exception("Нельзя одновременно указывать и NumberStart и Names");

                    if (config.ColCount.HasValue || config.RowCount.HasValue)
                    {
                        if (!config.ColCount.HasValue) throw new Exception("Не указан ColCount");
                        if (!config.RowCount.HasValue) throw new Exception("Не указан RowCount");

                        var colLength = config.ColLength ?? (config.ColCount.Value * formatSize);

                        if (colLength * config.RowCount.Value * formatSize > ushort.MaxValue - config.NumberStart.Value)
                            throw new Exception("Матрица регистров выходит за допустимые диапазоны адресации");

                        if (colLength < config.ColCount.Value * formatSize)
                            throw new Exception("ColLength не может быть меньше ColCount * <разме типа в регистрах>");

                        for (int col = 0; col < config.ColCount.Value; col++)
                        {
                            for (int row = 0; row < config.RowCount.Value; row++)
                            {
                                AddSingle(config, (ushort)(config.NumberStart.Value + col * formatSize + row * colLength), string.Format(config.Name, col, row), config.RegType.Value);
                            }
                        }
                    }
                    else
                    {
                        if (!config.NumberCount.HasValue) throw new Exception("Не указан NumberCount или ColCount и RowCount");

                        if (config.NumberCount * formatSize > ushort.MaxValue - config.NumberStart.Value)
                            throw new Exception("Массив регистров выходит за допустимые диапазоны адресации");

                        for (int ind = 0; ind < config.NumberCount.Value; ind++)
                        {
                            AddSingle(config, (ushort)(config.NumberStart.Value + ind * formatSize), string.Format(config.Name, ind), config.RegType.Value);
                        }
                    }
                }
                else if (config.Number.HasValue)
                {
                    AddSingle(config, config.Number.Value, config.Name, config.RegType.Value);
                }
                else
                {
                    throw new Exception("Не указаны параметры для получкния номера регистра (или регистр не найден по имени в родительстком тип)");
                }
            }
        }

        private class DeviceTypeResolver
        {
            public string TypeName { get; }
            public string? ParentTypeName { get; }

            private RegisterList? registerList = null;
            private ConfigConverter configConverter;
            public bool isInResolve = false;
            private bool resolved = false;
            private ModbusDeviceRegister[] regConfig;

            public DeviceTypeResolver(string typeName, string? parentTypeName, ConfigConverter configConverter, ModbusDeviceRegister[] regConfig)
            {
                TypeName = typeName;
                ParentTypeName = parentTypeName;
                this.configConverter = configConverter;
                this.regConfig = regConfig;
            }

            public RegisterList Resolve()
            {
                if (resolved)
                    return registerList!;

                if (isInResolve)
                    throw new Exception($"Циклическая зависимость среди родительских типов для {TypeName}");

                isInResolve = true;

                if (ParentTypeName != null)
                {
                    var parentType = configConverter.GetDeviceType(ParentTypeName);
                    var parentRegisters = parentType.Resolve();
                    registerList = new RegisterList(parentRegisters);
                }
                else
                {
                    registerList = new RegisterList();
                }

                foreach (var cfg in regConfig)
                {
                    registerList.AddRegister(cfg);
                }

                resolved = true;
                isInResolve = false;
                return registerList;
            }

        }

        private List<DeviceTypeResolver> deviceTypes = new List<DeviceTypeResolver>();

        private DeviceTypeResolver GetDeviceType(string name)
        {
            var type = deviceTypes.FirstOrDefault(t => t.TypeName == name);
            if (type == null)
                throw new Exception($"Тип устройства {name} не найден");

            return type;
        }

        public ConfigConverter(ModbudsDeviceType[] templates)
        {
            foreach(var tmpl in templates)
            {
                if (tmpl.TypeName == null)
                    throw new Exception("Не указан параметр TypeName для тиаа устройства");

                deviceTypes.Add(new DeviceTypeResolver(tmpl.TypeName, tmpl.ParentTypeName, this, tmpl.Registers));
            }
        }

        public List<RegisterSettings> ResolveRegisterList(string? typeName, ModbusDeviceRegister[] registers, string namePrefix)
        {
            RegisterList registerList;
            if (typeName != null)
            {
                var parentType = GetDeviceType(typeName);
                var parentRegisters = parentType.Resolve();
                registerList = new RegisterList(parentRegisters);
            }
            else
            {
                registerList = new RegisterList();
            }

            foreach (var cfg in registers)
            {
                registerList.AddRegister(cfg);
            }

            List<RegisterSettings> result = new();
            foreach(var r  in registerList.Registers)
            {
                result.Add(new RegisterSettings(r, namePrefix));
            }
            return result;
        }

    }
}
