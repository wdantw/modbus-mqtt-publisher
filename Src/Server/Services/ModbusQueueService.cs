using ModbusMqttPublisher.Server.Contracts.Settings;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Services.Modbus;
using System.Globalization;
using ModbusMqttPublisher.Server.Services.Queues;
using ModbusMqttPublisher.Server.Services.Publisher;
using ModbusMqttPublisher.Server.Services.Values;

namespace ModbusMqttPublisher.Server.Services
{
	public class ModbusQueueService : IQueueService
    {
        public record ReadTaskRequest
        (
            DeviceSettings Device,
            RegisterType RegType,
            int StartRegister,
            int RegisterCount,
            ArraySegment<RegisterSettings> Registers
        );

        private readonly PortSettings settings;
        private readonly ILogger<ModbusQueueService> logger;
        private readonly IModbusClientFactory modbusFactory;
        private readonly IMqttPublisher mqttPublisher;
        private readonly IWriteQueueService writeQueueService;

        ReadQueue<DeviceSettings, RegisterSettings> readQueue;

		public ModbusQueueService(
			PortSettings settings,
			ILogger<ModbusQueueService> logger,
			IModbusClientFactory modbusFactory,
			IMqttPublisher mqttPublisher,
			IWriteQueueService writeQueueService)
		{
			this.settings = settings;
			this.logger = logger;
			this.modbusFactory = modbusFactory;
			this.mqttPublisher = mqttPublisher;
			this.writeQueueService = writeQueueService;

            readQueue = new(settings.Devices);
		}

        private void PublishValue(string topic, string value)
        {
			var valStorage = new PublishValueStorageString(value);
			mqttPublisher.PublishTopic(new PublishCommand(topic, valStorage, false));
		}

		private void PublishValue(string topic, double value)
        {
			var valStorage = new PublishValueStorageDouble(value);
			mqttPublisher.PublishTopic(new PublishCommand(topic, valStorage, false));
		}

		private void PublishPrifilerData(Profiler profiler)
        {
            profiler.Stop(out var globalTime, out var methodsTimes);

            var serName = settings.SerialName;
            if (serName.StartsWith(MqttPath.TopicPathDelimeter))
                serName = serName[1..];

			var basePath = MqttPath.CombineTopicPath("ModbusMqttPublisher", serName);

			var format = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
			format.NumberDecimalSeparator = DefaultSettings.DecimalSeparator;

            PublishValue(MqttPath.CombineTopicPath(basePath, $"time_global"), globalTime.ToString());

			var otherTime = globalTime;

			foreach (var method in methodsTimes)
            {
                PublishValue(MqttPath.CombineTopicPath(basePath, $"percent_{method.Key}"), method.Value.TotalMilliseconds / globalTime.TotalMilliseconds * 100.0);
                PublishValue(MqttPath.CombineTopicPath(basePath, $"time_{method.Key}"), method.Value.ToString());
                otherTime = otherTime - method.Value;
			}

            PublishValue(MqttPath.CombineTopicPath(basePath, $"percent_other"), otherTime.TotalMilliseconds / globalTime.TotalMilliseconds * 100.0);
            PublishValue(MqttPath.CombineTopicPath(basePath, $"time_other"), otherTime.ToString());

			PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_readComands"), statReadCommands.ToString());
			PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_WriteCommands"), statWriteCommands.ToString());
			PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_ReadDataBytes"), statReadDataBytes.ToString());
			PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_WriteDataBytes"), statWriteDataBytes.ToString());

			PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_in_sec_readComands"), statReadCommands * 1.0 / globalTime.TotalSeconds);
			PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_in_sec_WriteCommands"), statWriteCommands * 1.0 / globalTime.TotalSeconds);
			PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_in_sec_ReadDataBytes"), statReadDataBytes * 1.0 / globalTime.TotalSeconds);
			PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_in_sec_WriteDataBytes"), statWriteDataBytes * 1.0 / globalTime.TotalSeconds);

			statReadCommands = 0;
			statWriteCommands = 0;
			statReadDataBytes = 0;
			statWriteDataBytes = 0;
			profiler.Start();
		}

		int statReadCommands = 0;
		int statWriteCommands = 0;
		int statReadDataBytes = 0;
		int statWriteDataBytes = 0;

		public async Task Run(CancellationToken cancellationToken)
        {
            using var modbusClient = modbusFactory.Create(settings);

            var profiler = new Profiler();
			profiler.Start();
			while (!cancellationToken.IsCancellationRequested)
            {
				logger.LogDebug($"Новый цикл работы с портом");

                if (profiler.Elapsed > TimeSpan.FromSeconds(5))
                    PublishPrifilerData(profiler);

				await modbusClient.CheckConnection(settings.ErrorSleepTimeout, cancellationToken);

				var writeQuery = profiler.WrapMethod("get_writes", () => writeQueueService.GetQuery(settings.SerialName));
                if (writeQuery != null)
                {
                    await profiler.WrapMethodAsync("write", () => PerfomWriteRequest(modbusClient, writeQuery));
                    writeQueueService.AcceptDequeued(settings.SerialName);
					await profiler.WrapMethodAsync("sleep", () => Task.Delay(settings.MinSleepTimeout, cancellationToken));
                    continue;
                }

                DateTime nextReadTime;

                using (var getReadProfilerHolder = profiler.StartMethod("get_reads"))
                {
					if (readQueue.GetReadTask(DateTime.Now, out var readTask, out var readDevice, out var readRegType, out nextReadTime))
					{
						getReadProfilerHolder.Dispose();

                        var readRequest = new ReadTaskRequest(
							readDevice,
                            readRegType.Value,
                            readTask.Value[0].Number,
                            readTask.Value[^1].EndRegisterNumber - readTask.Value[0].Number,
                            readTask.Value);

						await profiler.WrapMethodAsync("read", () => PerfomReadRequest(modbusClient, readRequest, profiler));
						await profiler.WrapMethodAsync("sleep", () => Task.Delay(settings.MinSleepTimeout, cancellationToken));

						continue;
					}
				}

				var delay = nextReadTime - DateTime.Now;
				
                if (delay > TimeSpan.Zero)
                {
					if (delay > TimeSpan.FromSeconds(5))
                        delay = TimeSpan.FromHours(5);

					logger.LogDebug($"Ожидание следующего чтения {delay}");

					await profiler.WrapMethodAsync("sleep", () => Task.WhenAny(writeQueueService.WaitForItems(settings.SerialName, cancellationToken), Task.Delay(delay)));
				}
            }
        }

        private async Task<bool> PerfomReadRequest(IModbusClient modbus, ReadTaskRequest readTask, Profiler profiler)
        {
            var readTime = DateTime.Now;

            if (readTask.RegType.IsBitReg())
            {
                bool[] readResult;

                try
                {
                    statReadCommands++;
                    statReadDataBytes += (readTask.RegisterCount + 7) / 8;

					using var holder = profiler.StartMethod("nmodbus_read");

					readResult = await modbus.ReadBitRegistersAsync(new ModbusRequest(
						readTask.Device.SlaveAddress,
						(ushort)readTask.StartRegister,
						(ushort)readTask.RegisterCount,
                        readTask.RegType,
                        readTask.Device.ReadRetryCount,
                        readTask.Device.ReadTimeout,
                        readTask.Device.WriteTimeout
					));
				}
				catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка чтения из Modbus");
                    readTask.Device.DeviceNextReadTime = DateTime.Now + readTask.Device.ErrorSleepTimeout;
                    return false;
				}

				readTask.Device.DeviceNextReadTime = DateTime.Now + readTask.Device.MinSleepTimeout;

                foreach(var reg in readTask.Registers)
                {
					var changed = reg.ReadFromModbus(readTime, readResult.AsSpan().Slice(reg.Number - readTask.StartRegister, 1));

					logger.LogDebug($"Для регистра {reg.Name} получены данные {reg.PublishValue}");

					if (changed || reg.ForcePublish)
					{
						mqttPublisher.PublishTopic(new PublishCommand(reg.Name, reg.PublishValue));
					}
                }
            }
            else
            {
                ushort[] readResult;

                try
                {
					statReadCommands++;
					statReadDataBytes += readTask.RegisterCount * 2;
					
					using var holder = profiler.StartMethod("nmodbus_read");

					readResult = await modbus.ReadShortRegistersAsync(new ModbusRequest(
						readTask.Device.SlaveAddress,
						(ushort)readTask.StartRegister,
						(ushort)readTask.RegisterCount,
						readTask.RegType,
						readTask.Device.ReadRetryCount,
						readTask.Device.ReadTimeout,
						readTask.Device.WriteTimeout
					));
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Ошибка чтения из Modbus");
					readTask.Device.DeviceNextReadTime = DateTime.Now + readTask.Device.ErrorSleepTimeout;
					return false;
				}

				readTask.Device.DeviceNextReadTime = DateTime.Now + readTask.Device.MinSleepTimeout;

				foreach (var reg in readTask.Registers)
				{
					var changed = reg.ReadFromModbus(readTime, readResult.AsSpan().Slice(reg.Number - readTask.StartRegister, reg.SizeInRegisters));

					logger.LogDebug($"Для регистра {reg.Name} получены данные {reg.PublishValue}");

					if (changed || reg.ForcePublish)
					{
						mqttPublisher.PublishTopic(new PublishCommand(reg.Name, reg.PublishValue));
					}
                }
            }

            return true;
        }

        private async Task<bool> PerfomWriteRequest(IModbusClient modbus, WriteQuery writeQuery)
        {
            (bool Found, DeviceSettings? Device, RegisterSettings? Register) FindRegister(WriteQuery writeQuery)
            {
                foreach (var dev in settings.Devices)
                {
                    foreach (var reg in dev.Registers)
                    {
                        if (reg.Name == writeQuery.TopicName)
                        {
							return (true, dev, reg);
						}
					}
                }
                return (false, null, null);
            }

			(var found, var tmp_device, var tmp_register) = FindRegister(writeQuery);

			if (!found)
				return false;

			var register = tmp_register!;
			var device = tmp_device!;

			if (register.RegType.IsBitReg())
			{
				var dataLength = register.EndRegisterNumber - register.Number;
				var data = new bool[dataLength];

				bool writeStarted = false;

				try
				{
					register.IncomeValueConverter.ToModbus(writeQuery.Value, data);
					logger.LogDebug($"Запись в modbus {register.Name}");
					register.SetNextReadTime(DateTime.MinValue);

					statWriteCommands++;
					statWriteDataBytes += (dataLength + 7) / 8;
					writeStarted = true;
					await modbus.WriteBitRegistersAsync(new ModbusRequest(
						device.SlaveAddress,
						register.Number,
						(ushort)dataLength,
						register.RegType,
						device.ReadRetryCount,
						device.ReadTimeout,
						device.WriteTimeout
					), data);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, $"Ошибка записи в устройство");
					if (writeStarted)
						device.DeviceNextReadTime = DateTime.Now + device.ErrorSleepTimeout;
					return false;
				}

				device.DeviceNextReadTime = DateTime.Now + device.MinSleepTimeout;
			}
			else
			{
				var dataLength = register.EndRegisterNumber - register.Number;
				var data = new ushort[dataLength];

				bool writeStarted = false;
				try
				{
					register.IncomeValueConverter.ToModbus(writeQuery.Value, data);
					logger.LogDebug($"Запись в modbus {register.Name}");
					register.SetNextReadTime(DateTime.MinValue);

					statWriteCommands++;
					statWriteDataBytes += dataLength * 2;
					writeStarted = true;
					await modbus.WriteShortRegistersAsync(new ModbusRequest(
						device.SlaveAddress,
						register.Number,
						(ushort)dataLength,
						register.RegType,
						device.ReadRetryCount,
						device.ReadTimeout,
						device.WriteTimeout
					), data);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, $"Ошибка записи в устройство");
					if (writeStarted)
						device.DeviceNextReadTime = DateTime.Now + device.ErrorSleepTimeout;
					return false;
				}

				device.DeviceNextReadTime = DateTime.Now + device.MinSleepTimeout;
			}

			return true;
		}
    }
}
