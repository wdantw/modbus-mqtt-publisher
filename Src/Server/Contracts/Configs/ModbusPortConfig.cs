using System.IO.Ports;

namespace ModbusMqttPublisher.Server.Contracts.Configs
{
    public class ModbusPortBase : ReadParameters
    {
        /// <summary>
        /// минимальное время между запросами.
        /// </summary>
        public TimeSpan? MinSleepTimeout { get; set; }

        /// <summary>
        /// Количество попыток чтения из Modbus.
        /// </summary>
        public int? RetryCount { get; set; }

        /// <summary>
        /// Время, через которое можно произвести следующую операцию с Modbus если предыдущая завершилась с ошибкой.
        /// </summary>
        public TimeSpan? ErrorSleepTimeout { get; set; }

        /// <summary>
        /// Таймаут операций чтения из Modbus
        /// </summary>
        public TimeSpan? ReadTimeout { get; set; }

        /// <summary>
        /// Таймаут операций записи в Modbus.
        /// </summary>
        public TimeSpan? WriteTimeout { get; set; }

        /// <summary>
        /// разрешены ли события Wirenboard.
        /// </summary>
        public bool? AllowWbEvents { get; set; }

        /// <summary>
        /// Имя порта. Если не указано будет использоваться SerialName
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Имя последовательного порта в системе
        /// </summary>
        public string? SerialName { get; set; }

        /// <summary>
        /// Скорость последовательного порта
        /// </summary>
        public int? BaudRate { get; set; }
        
        /// <summary>
        /// Количество бит данных последовательного порта
        /// </summary>
        public int? DataBits { get; set; }

        /// <summary>
        /// Четность последовательного порта
        /// </summary>
        public Parity? Parity { get; set; }

        /// <summary>
        /// Количество спот бит последовательного порта
        /// </summary>
        public StopBits? StopBits { get; set; }
	}

	public class ModbusPortConfig : ModbusPortBase
    {
        public ModbusDeviceConfig[] Devices { get; set; } = Array.Empty<ModbusDeviceConfig>();

		// переопределение свойств у всех регистров на порту
		public ModbusRegisterModifier[] Modifiers { get; set; } = Array.Empty<ModbusRegisterModifier>();
	}

    public class ModbusPortComplete : ModbusPortBase
    {
        public ModbusDeviceComplete[] Devices { get; set; } = Array.Empty<ModbusDeviceComplete>();
    }

    public class ModbusPorts : List<ModbusPortConfig>
    {
        public const string SectionName = "ModbusPorts";
    }

	public class ModbusModifiers : List<ModbusRegisterModifier>
	{
		public const string SectionName = "ModbusModifiers";
	}

}
