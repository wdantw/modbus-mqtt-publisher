using ModbusMqttPublisher.Server.Contracts.Settings;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Services.Modbus;
using System.Globalization;
using ModbusMqttPublisher.Server.Services.Values;
using ModbusMqttPublisher.Server.Services.Mqtt;
using ModbusMqttPublisher.Server.Domain;

namespace ModbusMqttPublisher.Server.Services
{
    public class ModbusQueueService : IQueueService
    {
        private readonly ReadPort settings;
        private readonly ILogger<ModbusQueueService> logger;
        private readonly IModbusClientFactory modbusFactory;
        private readonly IMqttBus mqttBus;
        private readonly IWriteQueueService writeQueueService;

		public ModbusQueueService(
            ReadPort settings,
			ILogger<ModbusQueueService> logger,
			IModbusClientFactory modbusFactory,
            IMqttBus mqttBus,
			IWriteQueueService writeQueueService)
		{
			this.settings = settings;
			this.logger = logger;
			this.modbusFactory = modbusFactory;
			this.mqttBus = mqttBus;
			this.writeQueueService = writeQueueService;
		}

        private async Task PublishValue(string topic, string value, CancellationToken cancellationToken)
        {
            var valStorage = new PublishValueStorageString(value);
            await mqttBus.EnqueueMessage(topic, valStorage.ToMqtt(), false, cancellationToken);
        }

        private async Task PublishValue(string topic, double value, CancellationToken cancellationToken)
        {
            var valStorage = new PublishValueStorageDouble(value);
            await mqttBus.EnqueueMessage(topic, valStorage.ToMqtt(), false, cancellationToken);
        }

		private async Task PublishPrifilerData(Profiler profiler, CancellationToken cancellationToken)
        {
            profiler.Stop(out var globalTime, out var methodsTimes);

            var serName = settings.SerialName;
            if (serName.StartsWith(MqttPath.TopicPathDelimeter))
                serName = serName[1..];

			var basePath = MqttPath.CombineTopicPath("ModbusMqttPublisher", serName);

			var format = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
			format.NumberDecimalSeparator = DefaultSettings.DecimalSeparator;

            await PublishValue(MqttPath.CombineTopicPath(basePath, $"time_global"), globalTime.ToString(), cancellationToken);

			var otherTime = globalTime;

			foreach (var method in methodsTimes)
            {
				var methodName = method.Key;

				if (methodName.StartsWith("$"))
				{
					methodName = methodName.Substring(1);
				}
				else
				{
					otherTime = otherTime - method.Value;
				}

                await PublishValue(MqttPath.CombineTopicPath(basePath, $"percent_{methodName}"), method.Value.TotalMilliseconds / globalTime.TotalMilliseconds * 100.0, cancellationToken);
                await PublishValue(MqttPath.CombineTopicPath(basePath, $"time_{methodName}"), method.Value.ToString(), cancellationToken);
			}

            await PublishValue(MqttPath.CombineTopicPath(basePath, $"percent_other"), otherTime.TotalMilliseconds / globalTime.TotalMilliseconds * 100.0, cancellationToken);
            await PublishValue(MqttPath.CombineTopicPath(basePath, $"time_other"), otherTime.ToString(), cancellationToken);

			await PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_readComands"), statReadCommands.ToString(), cancellationToken);
			await PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_WriteCommands"), statWriteCommands.ToString(), cancellationToken);
			await PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_ReadDataBytes"), statReadDataBytes.ToString(), cancellationToken);
            await PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_WriteDataBytes"), statWriteDataBytes.ToString(), cancellationToken);

			await PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_in_sec_readComands"), statReadCommands * 1.0 / globalTime.TotalSeconds, cancellationToken);
			await PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_in_sec_WriteCommands"), statWriteCommands * 1.0 / globalTime.TotalSeconds, cancellationToken);
			await PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_in_sec_ReadDataBytes"), statReadDataBytes * 1.0 / globalTime.TotalSeconds, cancellationToken);
            await PublishValue(MqttPath.CombineTopicPath(basePath, $"stat_in_sec_WriteDataBytes"), statWriteDataBytes * 1.0 / globalTime.TotalSeconds, cancellationToken);

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
			var profiler = new Profiler();
			using var modbusClient = modbusFactory.Create(settings, profiler);

			profiler.Start();
			while (!cancellationToken.IsCancellationRequested)
            {
				logger.LogDebug($"Новый цикл работы с портом");

                if (profiler.Elapsed > TimeSpan.FromSeconds(5))
                    await PublishPrifilerData(profiler, cancellationToken);

				await modbusClient.CheckConnection(settings.ErrorSleepTimeout, cancellationToken);

				var writeQuery = profiler.WrapMethod("get_writes", () => writeQueueService.GetQuery(settings.SerialName));
                if (writeQuery != null)
                {
                    await profiler.WrapMethodAsync("write", () => PerfomWriteRequest(modbusClient, writeQuery));
                    writeQueueService.AcceptDequeued(settings.SerialName);
					await profiler.WrapMethodAsync("sleep", () => Task.Delay(settings.MinSleepTimeout, cancellationToken));
                    continue;
                }

				var currTime = DateTime.Now;
                DateTime nextReadTime;

                using (var getReadProfilerHolder = profiler.StartMethod("get_reads"))
                {
					nextReadTime = settings.NextReadTime;

					if (nextReadTime <= currTime)
					{
						var readTask = settings.GetNextReadTask(currTime);

						if (readTask != null)
						{
                            await profiler.WrapMethodAsync("read", () => PerfomReadRequest(modbusClient, readTask, profiler, cancellationToken));
                            await profiler.WrapMethodAsync("sleep", () => Task.Delay(settings.MinSleepTimeout, cancellationToken));
                        }

                        continue;
                    }
				}

				var delay = nextReadTime - currTime;
				
                if (delay > TimeSpan.Zero)
                {
					if (delay > TimeSpan.FromSeconds(5))
                        delay = TimeSpan.FromHours(5);

					logger.LogDebug($"Ожидание следующего чтения {delay}");

					await profiler.WrapMethodAsync("sleep", () => writeQueueService.WaitForItems(settings.SerialName, cancellationToken).WithTimeoutNotThrow(delay));
				}
            }
        }

        private async Task<bool> PerfomReadRequest(IModbusClient modbus, ReadTask readTask, Profiler profiler, CancellationToken cancellationToken)
        {
            var readTime = DateTime.Now;

            if (readTask.RegisterType.IsBitReg())
            {
                bool[] readResult;

                try
                {
                    statReadCommands++;
                    statReadDataBytes += (readTask.RegisterCount + 7) / 8;

					using var holder = profiler.StartMethod("$nmodbus_read");

					readResult = await modbus.ReadBitRegistersAsync(new ModbusRequest(
						readTask.Device.SlaveAddress,
						readTask.StartNumber,
						readTask.RegisterCount,
                        readTask.RegisterType,
                        readTask.Device.ReadRetryCount,
                        readTask.Device.ReadTimeout,
                        readTask.Device.WriteTimeout
					));

					holder.Dispose();
				}
				catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка чтения из Modbus");
					readTask.AccessFailed(DateTime.Now);
                    return false;
				}

                foreach(var reg in readTask.Registers)
                {
					var needPublish = reg.ReadFromModbus(readTime, readResult.AsSpan().Slice(reg.StartNumber - readTask.StartNumber, 1));

					logger.LogDebug("Для регистра {regName} получены данные {value}", reg.Name, reg.PublishValue);

					if (needPublish)
					{
						await mqttBus.EnqueueMessage(reg.Name, reg.PublishValue.ToMqtt(), true, cancellationToken);
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
					
					using var holder = profiler.StartMethod("$nmodbus_read");

					readResult = await modbus.ReadShortRegistersAsync(new ModbusRequest(
						readTask.Device.SlaveAddress,
						readTask.StartNumber,
						readTask.RegisterCount,
						readTask.RegisterType,
						readTask.Device.ReadRetryCount,
						readTask.Device.ReadTimeout,
						readTask.Device.WriteTimeout
					));

					holder.Dispose();
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Ошибка чтения из Modbus");
                    readTask.AccessFailed(DateTime.Now);
                    return false;
				}

				foreach (var reg in readTask.Registers)
				{
					var needPubliish = reg.ReadFromModbus(readTime, readResult.AsSpan().Slice(reg.StartNumber - readTask.StartNumber, reg.SizeInRegisters));

                    logger.LogDebug("Для регистра {regName} получены данные {value}", reg.Name, reg.PublishValue);

                    if (needPubliish)
					{
						await mqttBus.EnqueueMessage(reg.Name, reg.PublishValue.ToMqtt(), true, cancellationToken);
					}
                }
            }

            return true;
        }

        private async Task<bool> PerfomWriteRequest(IModbusClient modbus, WriteQuery writeQuery)
        {
			var writeTask = settings.GetWriteTask(writeQuery.TopicName);

			if (writeTask == null)
				return false;

			var register = writeTask.Register;
			var device = writeTask.Device;


            if (register.RegisterType.IsBitReg())
			{
				var dataLength = register.EndNumber - register.StartNumber;
				var data = new bool[dataLength];

				bool writeStarted = false;

				try
				{

					register.IncomeValueConverter.ToModbus(writeQuery.Value, data);
                    logger.LogDebug("Запись в modbus {registerName}", register.Name);

                    statWriteCommands++;
					statWriteDataBytes += (dataLength + 7) / 8;
					
					writeStarted = true;

					await modbus.WriteBitRegistersAsync(new ModbusRequest(
                        device.SlaveAddress,
						register.StartNumber,
						(ushort)dataLength,
						register.RegisterType,
                        device.ReadRetryCount,
                        device.ReadTimeout,
                        device.WriteTimeout
					), data);

                    register.ValueWritedToDevice(DateTime.Now);
                }
				catch (Exception ex)
				{
					logger.LogError(ex, $"Ошибка записи в устройство");

					if (writeStarted)
						register.AccessFailed(DateTime.Now);

					return false;
				}
			}
			else
			{
				var dataLength = register.EndNumber - register.StartNumber;
				var data = new ushort[dataLength];

				bool writeStarted = false;
				try
				{
					register.IncomeValueConverter.ToModbus(writeQuery.Value, data);
                    logger.LogDebug("Запись в modbus {registerName}", register.Name);

                    statWriteCommands++;
					statWriteDataBytes += dataLength * 2;
					writeStarted = true;
					await modbus.WriteShortRegistersAsync(new ModbusRequest(
                        device.SlaveAddress,
						register.StartNumber,
						(ushort)dataLength,
						register.RegisterType,
                        device.ReadRetryCount,
                        device.ReadTimeout,
                        device.WriteTimeout
					), data);

                    register.ValueWritedToDevice(DateTime.Now);
                }
				catch (Exception ex)
				{
					logger.LogError(ex, $"Ошибка записи в устройство");
                    
					if (writeStarted)
                        register.AccessFailed(DateTime.Now);
                    
					return false;
				}
			}

			return true;
		}
    }
}
