using ModbusMqttPublisher.Server.Contracts.Configs.Enums;

namespace ModbusMqttPublisher.Server.Contracts.Configs
{
    public class ModbusRegisterCommonParams
    {
        /// <summary>
        /// множитель для получения результирующего значения
        /// </summary>
        public double? Scale { get; set; }

        /// <summary>
        /// количество знаков после запятой. Допустимо использовать только с параметром Scale
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// указывает, что данный регистр генерирует события WirenBoard и его можно не опрашивать стандатным образом
        /// </summary>
        public bool? WbEvents { get; set; }

        /// <summary>
        /// период опроса регистра
        /// </summary>
        public TimeSpan? ReadPeriod { get; set; }

        /// <summary>
        /// теги, указанные через запятую. если это наследование и отсутсвует Names, то по ним выбираются все подходящие родительские регистры
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// наименование топика в Mqtt (относительное, в дальшейнем будет возможность указать абсолютное или даже использовать шаблон)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Разделитель дробной и целой части.
        /// </summary>
        public string? DecimalSeparator { get; set; }

        /// <summary>
        /// Минимальная разница, для чисел с плавающей точкой, меньше которой изменения игнорируются.
        /// </summary>
        public double? CompareDiff { get; set; }

        /// <summary>
        /// Публиковать данные в Mqtt после каждого чтения из устройства, даже если заначение не изменилось.
        /// </summary>
        public bool? ForcePublish { get; set; }
	}

    public class ModbusRegisterModifier : ModbusRegisterCommonParams
    {
        /// <summary>
        /// при наследовании типов указывается регулярное выражение, чтобы применить настройки ко всем подходящим родительским регистрам
        /// </summary>
        public string? Names { get; set; }
    }

    public class ModbusRegisterCompleted : ModbusRegisterCommonParams
    {
        /// <summary>
        /// Тип и формат данных регистра
        /// </summary>
        public ConfigRegisterType? RegType { get; set; }

        /// <summary>
        /// Длина строки для строковых параметров
        /// </summary>
        public byte? Length { get; set; }

        /// <summary>
        /// Номер регистра (начальный для данных из нескольких регистров)
        /// </summary>
        public ushort? Number { get; set; }
    }

    public class ModbusRegisterTemplate : ModbusRegisterCompleted
    {
        /// <summary>
        /// Номер первого регистра в массиве или матрице
        /// </summary>
        public ushort? NumberStart { get; set; }

        /// <summary>
        /// Шаг в регистрах для массива или "ряда" матрицы. если не указан, то принимается вычисленному размеру по типу регистра
        /// </summary>
        public byte? NumberStep { get; set; }

        /// <summary>
        /// Количество элементов в массиве
        /// </summary>
        public byte? NumberCount { get; set; }

        /// <summary>
        /// Количество "колонок" в матрице
        /// </summary>
        public byte? ColCount { get; set; }

        /// <summary>
        /// длина одного "ряда" в матрице в регистрах (если отсутсвует, то вычисляется на основе ColCount и формата регистра)
        /// </summary>
        public byte? RowLength { get; set; }

        /// <summary>
        /// количество "рядов" в матрице
        /// </summary>
        public byte? RowCount { get; set; }
    }
}
