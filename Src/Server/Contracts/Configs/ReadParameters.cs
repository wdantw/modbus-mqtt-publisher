namespace ModbusMqttPublisher.Server.Contracts.Configs
{
    public class ReadParameters
    {
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
	}
}
