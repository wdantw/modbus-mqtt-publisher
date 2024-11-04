namespace ModbusMqttPublisher.Server.Services.Values
{
	public interface IPublishValueSorage
	{
		byte[] ToMqtt();
	}

	public interface IRegisterValueStorage : IPublishValueSorage
	{
		bool FromModbus(ReadOnlySpan<ushort> data);
		
		/// <summary>
		/// Преобразует данные, полученные из modbus в коенчный формат
		/// </summary>
		/// <param name="data"></param>
		/// <returns>Возвращает true если данные были изменены</returns>
		bool FromModbus(ReadOnlySpan<bool> data);
	}

	public interface INumericRegisterValueStorage : IRegisterValueStorage
	{
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
