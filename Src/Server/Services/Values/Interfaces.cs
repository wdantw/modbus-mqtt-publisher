namespace ModbusMqttPublisher.Server.Services.Values
{
	public interface IPublishValueSorage
	{
		/// <summary>
		/// Возвращает данные, в формате для Mqtt
		/// </summary>
		/// <returns></returns>
		byte[] ToMqtt();
	}

	public interface IRegisterValueStorage : IPublishValueSorage
	{
        /// <summary>
        /// Сохраняет данные, полученные из modbus.
        /// </summary>
        /// <param name="data">данные из modbus</param>
        /// <returns>Возвращает true если данные были изменены</returns>
        bool FromModbus(ReadOnlySpan<ushort> data);

        /// <summary>
        /// Сохраняет данные полученные из modbus.
        /// </summary>
        /// <param name="data">данные из modbus</param>
        /// <returns>Возвращает true если данные были изменены</returns>
        bool FromModbus(ReadOnlySpan<bool> data);
	}

	public interface INumericRegisterValueStorage : IRegisterValueStorage
	{
		/// <summary>
		/// Возвращает данные, преобразованные к числу с плавающей точкой.
		/// </summary>
		/// <returns></returns>
		double ToDouble();
	}

	public interface IIncomeRegisterConverter
	{
		void ToModbus(ReadOnlySpan<byte> mqttData, Span<ushort> modbusData);
		void ToModbus(ReadOnlySpan<byte> mqttData, Span<bool> modbusData);
	}

	public interface IRegisterValueStorageWithInConverter : IRegisterValueStorage, IIncomeRegisterConverter
	{

	}

	public interface INumericRegisterValueStorageWithInConverter : INumericRegisterValueStorage, IRegisterValueStorageWithInConverter
	{
		void ToModbus(double doubleValue, Span<ushort> modbusData);
	}
}
