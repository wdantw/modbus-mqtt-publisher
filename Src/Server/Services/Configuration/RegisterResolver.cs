using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Configs;

namespace ModbusMqttPublisher.Server.Services.Configuration
{
    public static class RegisterResolver
    {
        public static IEnumerable<ModbusRegisterCompleted> ResolveRegisters(this ModbusRegisterTemplate register)
        {
            bool isCertain = register.Number.HasValue;
            bool isArray = register.NumberStart.HasValue && register.NumberCount.HasValue;
            bool isMatrix = register.NumberStart.HasValue && (register.ColCount.HasValue || register.RowLength.HasValue || register.RowCount.HasValue);

            if ((isCertain ? 1 : 0) + (isArray ? 1 : 0) + (isMatrix ? 1 : 0) != 1)
                throw new Exception("Адрес регистра должен определятся единственным способом - явно, как массив или как матрица");

            if (isCertain)
            {
                if (register.NumberStep.HasValue)
                    throw new Exception("Для явно указанного адреса регистра нельзя указывать NumberStep");

				var name = register.Name ?? throw new Exception($"Не указано имя для регистра {register.Number!.Value}");
				yield return ResolveSingleRegister(register, register.Number!.Value, name);
            }
            else if (isArray)
            {
                var regSize = register.CalcRegisgrerSize();
                var step = register.NumberStep ?? regSize;
                if (step < regSize)
                    throw new Exception("Шаг в массиве не может быть меньше, чем размер регистра");

				var name = register.Name ?? throw new Exception($"Не указано имя для массива {register.NumberStart!.Value}");
				
                for (int i = 0; i < register.NumberCount!.Value; i++)
                {
                    var number = register.NumberStart!.Value + i * step;

                    if (number > ushort.MaxValue)
                        throw new Exception($"Некорректные параметры массива - адрес регистра превысили значение {ushort.MaxValue}");

                    yield return ResolveSingleRegister(register, (ushort)number, string.Format(name, i + 1));
                }
            }
            else
            {
                if (!register.ColCount.HasValue)
                    throw new Exception("Не указан параметр ColCount для матрицы");

                if (!register.RowCount.HasValue)
                    throw new Exception("Не указан параметр ColCount для матрицы");

                var regSize = register.CalcRegisgrerSize();

                var step = register.NumberStep ?? regSize;

                if (step < regSize)
                    throw new Exception("Шаг в матрице не может быть меньше, чем размер регистра");

                var defaultRowLength = step * register.ColCount.Value;
                var minRowLength = defaultRowLength - step + regSize;
                var colLength = register.RowLength ?? defaultRowLength;

                if (colLength < minRowLength)
                    throw new Exception("Указан размер строки матрицы меньший чем минимальный с учетом размера регистра и шага");

				var name = register.Name ?? throw new Exception($"Не указано имя для матрицы {register.NumberStart!.Value}");

				for (int col = 0; col < register.ColCount.Value; col++)
                {
                    for (int row = 0; row < register.RowCount.Value; row++)
                    {
                        var number = register.NumberStart!.Value + row * colLength + col * step;

                        if (number > ushort.MaxValue)
                            throw new Exception($"Некорректные параметры массива - адрес регистра превысили значение {ushort.MaxValue}");

                        yield return ResolveSingleRegister(register, (ushort)number, string.Format(name, col + 1, row + 1));
                    }
                }
            }

        }

        private static byte CalcRegisgrerSize(this ModbusRegisterCompleted register)
        {
            if (!register.RegType.HasValue)
                throw new Exception("Для вычисления размера регистра должен быть указан RegType");

            var type = register.RegType.Value.GetRegisterType();
            var format = register.RegType.Value.GetRegisterFormat();
            byte length = 1;

            if (format.FormatHasLength())
            {
                if (!register.Length.HasValue)
                    throw new Exception("Для данного формата регистра необходимо указать длину");

                length = register.Length.Value;
            }
            else
            {
                if (register.Length.HasValue)
                    throw new Exception("Для данного формата регистра наличие длины недопустимо");
            }

            var size = length * format.SizeInRegisters();
            if (size > byte.MaxValue)
                throw new Exception($"Размер регистра не может превышать значение {byte.MaxValue}");

            return (byte)size;
        }

        private static ModbusRegisterCompleted ResolveSingleRegister(ModbusRegisterCompleted config, ushort number, string name)
        {
            var dest = new ModbusRegisterCompleted();
            dest.MergeRegisterCommonParams(config);
            dest.RegType = config.RegType ?? dest.RegType;
            dest.Length = config.Length ?? dest.Length;
            dest.Number = number;
            dest.Name = name;

            return dest;
        }
    }
}
