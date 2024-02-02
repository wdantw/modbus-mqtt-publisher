namespace MudbusMqttPublisher.Server.Contracts.Configs
{
    public class ReadParameters
    {
        // минимальное время между запросами
        public TimeSpan? MinSleepTimeout { get; set; }

        // разрешены ли события Wirenboard
        public bool? AllowWbEvents { get; set; }

        // максимально допустимое количество ненужных для чтения идущих подряд 16ти-битных регистров в рамках одного запроса на чтение
        public int? MaxRegHole { get; set; }

        // максимально допустимое количество ненужных для чтения идущих подряд однобитных регистров в рамках одного запроса на чтение
        public int? MaxBitHole { get; set; }

        // максимально допустимое количество 16 битных регистров для чтения в одном запросе
        public int? MaxReadRegisters { get; set; }

        // максимально допустимое количество однобитных регистров для чтения в одном запросе (
        public int? MaxReadBit { get; set; }

		public TimeSpan? ReadTimeout { get; set; }

		public TimeSpan? WriteTimeout { get; set; }

		public int? WriteRetryCount { get; set; }

		public int? ReadRetryCount { get; set; }

		public TimeSpan? ErrorSleepTimeout { get; set; }
	}
}
