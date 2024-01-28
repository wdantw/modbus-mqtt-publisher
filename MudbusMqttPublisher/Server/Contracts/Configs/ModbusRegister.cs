namespace MudbusMqttPublisher.Server.Contracts.Configs
{
    public class ModbusRegisterCommonParams
    {
        // множитель для получения результирующего значения
        public double? Scale { get; set; }

        // количество знаков после запятой. Допустимо использовать только с параметром Scale
        public int? Precision { get; set; }

        // указывает, что данный регистр генерирует события WirenBoard и его можно не опрашивать стандатным образом
        public bool? WbEvents { get; set; }

        // период опроса регистра
        public TimeSpan? ReadPeriod { get; set; }

        // теги, указанные через запятую. если это наследование и отсутсвует Names, то по ним выбираются все подходящие родительские регистры
        public string? Tags { get; set; }

        // наименование топика в Mqtt (относительное, в дальшейнем будет возможность указать абсолютное или даже использовать шаблон)
        public string? Name { get; set; }

        public string? DecimalSeparator { get; set; }

        public double? CompareDiff { get; set; }
	}

    public class ModbusRegisterModifier : ModbusRegisterCommonParams
    {
        // при наследовании типов указывается регулярное выражение, чтобы применить настройки ко всем подходящим родительским регистрам
        public string? Names { get; set; }
    }

    public class ModbusRegisterCompleted : ModbusRegisterCommonParams
    {
        public ConfigRegisterType? RegType { get; set; }

        // длина строки
        public byte? Length { get; set; }

        // Номер регистра
        public ushort? Number { get; set; }
    }

    public class ModbusRegisterTemplate : ModbusRegisterCompleted
    {
        // Номер первого регистра в массиве или матрице
        public ushort? NumberStart { get; set; }

        // Шаг в регистрах для массива или "ряда" матрицы. если не указан, то принимается вычисленному размеру по типу регистра
        public byte? NumberStep { get; set; }

        // Количество элементов в массиве
        public byte? NumberCount { get; set; }

        // Количество "колонок" в матрице
        public byte? ColCount { get; set; }

        // длина одного "ряда" в матрице в регистрах
        public byte? RowLength { get; set; }

        // количество "рядов" в матрице
        public byte? RowCount { get; set; }
    }
}
