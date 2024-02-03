using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Settings;
using NModbus;
using NModbus.Serial;
using System.IO.Ports;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
	public record ModbusRequest(
		byte SlaveAddress,
		ushort StartRegister,
		ushort RegisterCount,
		RegisterType RegisterType,
		int RetryCount,
		TimeSpan ReadTimeout,
		TimeSpan WriteTimeout
		);

	public class ModbusClient : IModbusClient
	{
		IModbusMaster modbusMaster;
		ILogger<ModbusClient> logger;
		SerialPort serialPort;
		bool isFirstConnect = true;

		public ModbusClient(PortSettings settings, IModbusFactory modbusFactory, ILogger<ModbusClient> logger)
		{
			this.logger = logger;

			serialPort = new SerialPort(settings.SerialName);
			serialPort.BaudRate = settings.BaudRate;
			serialPort.DataBits = settings.DataBits;
			serialPort.Parity = settings.Parity;
			serialPort.StopBits = settings.StopBits;

			modbusMaster = modbusFactory.CreateRtuMaster(serialPort);
		}

		public void Dispose()
		{
			modbusMaster.Dispose();
			serialPort.Dispose();
		}

		public bool IsPortOpen => serialPort.IsOpen;

		public async Task CheckConnection(TimeSpan reconnectTimeout, CancellationToken cancellationToken)
		{
			if (serialPort.IsOpen)
				return;

			while (true)
			{
				if (isFirstConnect)
				{
					logger.LogDebug($"Соединение с портом {serialPort.PortName}");
				}
				else
				{
					logger.LogWarning($"Порт {serialPort.PortName} закрыт. Попытка соединения.");
				}

				try
				{
					serialPort.Open();
				}
				catch (Exception ex)
				{
					logger.LogError(ex, $"Ошибка открытия порта {serialPort.PortName}");
				}

				if (serialPort.IsOpen)
					return;

				logger.LogDebug($"Следующая попытка открытия порта {serialPort.PortName} через {reconnectTimeout}");

				await Task.Delay(reconnectTimeout, cancellationToken);
			}
		}

		private void InitRequest(string msg, ModbusRequest request)
		{
			logger.LogTrace($"{msg}. Адрес {request.SlaveAddress}, Тип Рег: {request.RegisterType}, Номер рег: {request.StartRegister}, Кол-во: {request.RegisterCount}");

			modbusMaster.Transport.Retries = request.RetryCount;
			modbusMaster.Transport.ReadTimeout = (int)request.ReadTimeout.TotalMilliseconds;
			modbusMaster.Transport.WriteTimeout = (int)request.WriteTimeout.TotalMilliseconds;
		}

		private Exception CreteExeption(Exception inner, string message, ModbusRequest request)
		{
			return new Exception($"{message}. Адрес {request.SlaveAddress}, Тип Рег: {request.RegisterType}, Номер рег: {request.StartRegister}, Кол-во: {request.RegisterCount}", inner);
		}

		public async Task<bool[]> ReadBitRegistersAsync(ModbusRequest request)
		{
			InitRequest("Чтение из modbus", request);

			try
			{
				switch (request.RegisterType)
				{
					case RegisterType.Coil:
						return await modbusMaster.ReadCoilsAsync(request.SlaveAddress, request.StartRegister, request.RegisterCount);
					case RegisterType.DiscreteInput:
						return await modbusMaster.ReadInputsAsync(request.SlaveAddress, request.StartRegister, request.RegisterCount);
					default:
						throw new NotImplementedException();
				}
			}
			catch (Exception ex)
			{
				throw CreteExeption(ex, "Ошибка чтения из Modbus", request);
			}
		}

		public async Task<ushort[]> ReadShortRegistersAsync(ModbusRequest request)
		{
			InitRequest("Чтение из modbus", request);

			try
			{
				switch (request.RegisterType)
				{
					case RegisterType.HoldingRegister:
						return await modbusMaster.ReadHoldingRegistersAsync(request.SlaveAddress, request.StartRegister, request.RegisterCount);
					case RegisterType.InputRegister:
						return await modbusMaster.ReadInputRegistersAsync(request.SlaveAddress, request.StartRegister, request.RegisterCount);
					default:
						throw new NotImplementedException();
				}
			}
			catch (Exception ex)
			{
				throw CreteExeption(ex, "Ошибка чтения из Modbus", request);
			}
		}

		public async Task WriteBitRegistersAsync(ModbusRequest request, bool[] data)
		{
			InitRequest("Запись в modbus", request);

			try
			{

				if (request.RegisterType != RegisterType.Coil)
					throw new Exception($"Нельзя писать в регистр {request.RegisterType}");

				if (request.RegisterCount != data.Length)
					throw new Exception($"Количество переданных данных не соответсвует запрошенному количесву регистров для записи");

				if (request.RegisterCount == 1)
				{
					await modbusMaster.WriteSingleCoilAsync(request.SlaveAddress, request.StartRegister, data[0]);
				}
				else
				{
					await modbusMaster.WriteMultipleCoilsAsync(request.SlaveAddress, request.StartRegister, data);
				}
			}
			catch (Exception ex)
			{
				throw CreteExeption(ex, "Ошибка записи в Modbus", request);
			}
		}

		public async Task WriteShortRegistersAsync(ModbusRequest request, ushort[] data)
		{
			InitRequest("Запись в modbus", request);

			try
			{
				if (request.RegisterType != RegisterType.HoldingRegister)
					throw new Exception($"Нельзя писать в регистр {request.RegisterType}");

				if (request.RegisterCount != data.Length)
					throw new Exception($"Количество переданных данных не соответсвует запрошенному количесву регистров для записи");

				if (request.RegisterCount == 1)
				{
					await modbusMaster.WriteSingleRegisterAsync(request.SlaveAddress, request.StartRegister, data[0]);
				}
				else
				{
					await modbusMaster.WriteMultipleRegistersAsync(request.SlaveAddress, request.StartRegister, data);
				}
			}
			catch (Exception ex)
			{
				throw CreteExeption(ex, "Ошибка записи в Modbus", request);
			}
		}

	}
}
