namespace ModbusMqttPublisher.Server.Contracts.Configs
{
    public class ReadParameters
    {
        /// <summary>
        /// минимальное время между запросами.
        /// </summary>
        public TimeSpan? MinSleepTimeout { get; set; }

        /// <summary>
        /// разрешены ли события Wirenboard.
        /// </summary>
        public bool? AllowWbEvents { get; set; }

        /// <summary>
        /// максимально допустимое количество ненужных для чтения идущих подряд 16ти-битных регистров в рамках одного запроса на чтение.
        /// </summary>
        public int? MaxRegHole { get; set; }

        /// <summary>
        /// максимально допустимое количество ненужных для чтения идущих подряд однобитных регистров в рамках одного запроса на чтение.
        /// </summary>
        public int? MaxBitHole { get; set; }

        /// <summary>
        /// максимально допустимое количество 16 битных регистров для чтения в одном запросе.
        /// </summary>
        public int? MaxReadRegisters { get; set; }

        /// <summary>
        /// максимально допустимое количество однобитных регистров для чтения в одном запросе.
        /// </summary>
        public int? MaxReadBit { get; set; }

		/// <summary>
        /// Таймаут операций чтения из Modbus
        /// </summary>
        public TimeSpan? ReadTimeout { get; set; }

        /// <summary>
        /// Таймаут операций записи в Modbus.
        /// </summary>
        public TimeSpan? WriteTimeout { get; set; }

        /// <summary>
        /// Количество попыток записи в Modbus.
        /// </summary>
        public int? WriteRetryCount { get; set; }

        /// <summary>
        /// Количество попыток чтения из Modbus.
        /// </summary>
        public int? ReadRetryCount { get; set; }

		/// <summary>
        /// Время, через которое можно произвести следующую операцию с Modbus если предыдущая завершилась с ошибкой.
        /// </summary>
        public TimeSpan? ErrorSleepTimeout { get; set; }
	}
}
