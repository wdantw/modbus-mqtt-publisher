using MudbusMqttPublisher.Server.Contracts.Settings;
using MudbusMqttPublisher.Server.Common;
using MudbusMqttPublisher.Server.Contracts;
using MudbusMqttPublisher.Server.Services.Types;
using MudbusMqttPublisher.Server.Services.Modbus;
using System.Globalization;
using MudbusMqttPublisher.Server.Services.Queues;

namespace MudbusMqttPublisher.Server.Services
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
        private readonly ITopicStateService topickStateService;
        private readonly IMqttPublisher mqttPublisher;
        private readonly IWriteQueueService writeQueueService;
        private readonly IRegisterValueFactory registerValueFactory;

        ReadQueue<DeviceSettings, RegisterSettings> readQueue;

		public ModbusQueueService(
			PortSettings settings,
			ILogger<ModbusQueueService> logger,
			IModbusClientFactory modbusFactory,
			ITopicStateService topickStateService,
			IMqttPublisher mqttPublisher,
			IWriteQueueService writeQueueService,
			IRegisterValueFactory registerValueFactory)
		{
			this.settings = settings;
			this.logger = logger;
			this.modbusFactory = modbusFactory;
			this.topickStateService = topickStateService;
			this.mqttPublisher = mqttPublisher;
			this.writeQueueService = writeQueueService;
			this.registerValueFactory = registerValueFactory;

            readQueue = new(settings.Devices);
		}

        private void PublishValue(string topic, string value)
        {
			topickStateService.UpdateTopicState(new TopickStateDto(
				topic,
				new StringPublishedValue(value),
				DateTime.Now
				));

			mqttPublisher.PublishTopic(topic);
		}

		private void PublishValue(string topic, double value)
        {
			var format = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
			format.NumberDecimalSeparator = DefaultSettings.DecimalSeparator;
            PublishValue(topic, value.ToString(format));
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

				var writeQueries = profiler.WrapMethod("get_writes", () => writeQueueService.GetQueries(settings.SerialName));
                if (writeQueries.Length > 0)
                {
                    await profiler.WrapMethodAsync("write", () => PerfomWriteRequest(modbusClient, writeQueries));
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

						await profiler.WrapMethodAsync("read", () => PerfomReadRequest(modbusClient, readRequest));
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

        private async Task<bool> PerfomReadRequest(IModbusClient modbus, ReadTaskRequest readTask)
        {
            var readTime = DateTime.Now;

            if (readTask.RegType.IsBitReg())
            {
                bool[] readResult;

                try
                {
                    statReadCommands++;
                    statReadDataBytes += readTask.RegisterCount / 8 + (readTask.RegisterCount % 8 > 0 ? 1 : 1);

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
					reg.SetNextReadTime(reg.ReadPeriod.HasValue ? readTime + reg.ReadPeriod.Value : DateTime.MaxValue);
					var regValue = registerValueFactory.Create(reg);
                    regValue.FromModbus(readResult.GetSegment(reg.Number - readTask.StartRegister, 1));

					logger.LogDebug($"Для регистра {reg.Name} получены данные {regValue}");
                    if (topickStateService.UpdateTopicState(new TopickStateDto(reg.Name, regValue, readTime)))
                    {
                        logger.LogDebug($"Для регистра {reg.Name} данные добавлены в очередь отправки");
                        mqttPublisher.PublishTopic(reg.Name);
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

				int endRegister = readTask.StartRegister + readTask.RegisterCount;

				foreach (var reg in readTask.Registers)
				{
					reg.SetNextReadTime(reg.ReadPeriod.HasValue ? readTime + reg.ReadPeriod.Value : DateTime.MaxValue);
					var regValue = registerValueFactory.Create(reg);
					regValue.FromModbus(readResult.GetSegment(reg.Number - readTask.StartRegister, reg.SizeInRegisters));

                    logger.LogDebug($"Для регистра {reg.Name} получены данные {regValue}");
                    if (topickStateService.UpdateTopicState(new TopickStateDto(reg.Name, regValue, readTime)))
                    {
                        logger.LogDebug($"Для регистра {reg.Name} данные добавлены в очередь отправки");
                        mqttPublisher.PublishTopic(reg.Name);
                    }
                }
            }

            return true;
        }

        private async Task<bool> PerfomWriteRequest(IModbusClient modbus, WriteQuery[] queries)
        {
            (bool Found, DeviceSettings? Device, RegisterSettings? Register, IRegisterValue? Value) FindRegister(WriteQuery writeQuery)
            {
                foreach (var dev in settings.Devices)
                {
                    foreach (var reg in dev.Registers)
                    {
                        if (reg.Name == writeQuery.TopicName)
                        {
                            var regValue = registerValueFactory.Create(reg);
                            regValue.FromMqtt(writeQuery.Value);
							return (true, dev, reg, regValue);
						}
					}
                }
                return (false, null, null, null);
            }

            bool result = true;

			var grouped = queries
                .Select(FindRegister)
                .Where(r => r.Found)
                .Select(r => new { Device = r.Device!, Register = r.Register!, Value = r.Value! })
                .GroupBy(r => new { addr = r.Device.SlaveAddress, regType = r.Register.RegType })
                ;

            foreach(var group in grouped)
            {
				var groupArr = group.OrderBy(r => r.Register.Number).ToArray();
                var startInd = 0;
                var maxBatchSize = groupArr[0].Register.RegType.IsBitReg() ? groupArr[0].Device.MaxReadBit : groupArr[0].Device.MaxReadRegisters;

                while (startInd < groupArr.Length)
                {
                    var endInd = startInd + 1;
                    while (
                        endInd < groupArr.Length &&
                        groupArr[endInd - 1].Register.EndRegisterNumber == groupArr[endInd].Register.Number &&
                        groupArr[endInd].Register.EndRegisterNumber - groupArr[startInd].Register.Number <= maxBatchSize
                        )
                    {
                        endInd++;
                    }

                    if (groupArr[0].Register.RegType.IsBitReg())
                    {
						var firstRegister = groupArr[startInd].Register;

						var dataLength = groupArr[endInd - 1].Register.EndRegisterNumber - firstRegister.Number;
						var data = new bool[dataLength];

						for (var regInd = startInd; regInd < endInd; regInd++)
						{
							var currReg = groupArr[regInd];
							currReg.Value.ToModbus(data.GetSegment(currReg.Register.Number - firstRegister.Number, currReg.Register.SizeInRegisters));
                            logger.LogDebug($"Запись в modbus {currReg.Register.Name} = {currReg.Value}");
							groupArr[regInd].Register.SetNextReadTime(DateTime.MinValue);
						}

						try
                        {
                            statWriteCommands++;
                            statWriteDataBytes += dataLength / 8 + (dataLength % 8 > 0 ? 1 : 1);
							await modbus.WriteBitRegistersAsync(new ModbusRequest(
								groupArr[0].Device.SlaveAddress,
								groupArr[startInd].Register.Number,
								(ushort)dataLength,
								firstRegister.RegType,
								groupArr[0].Device.ReadRetryCount,
								groupArr[0].Device.ReadTimeout,
								groupArr[0].Device.WriteTimeout
							), data); 
                            
                            result = true;
						}
						catch (Exception ex)
                        {
							logger.LogError(ex, $"Ошибка записи в устройство");
							result = false;
						}
					}
					else
                    {
                        var firstRegister = groupArr[startInd].Register;

						var dataLength = groupArr[endInd - 1].Register.EndRegisterNumber - firstRegister.Number;
                        var data = new ushort[dataLength];

                        for (var regInd = startInd; regInd < endInd; regInd++)
                        {
                            var currReg = groupArr[regInd];
                            currReg.Value.ToModbus(data.GetSegment(currReg.Register.Number - firstRegister.Number, currReg.Register.SizeInRegisters));
							logger.LogDebug($"Запись в modbus {currReg.Register.Name} = {currReg.Value}");
							groupArr[regInd].Register.SetNextReadTime(DateTime.MinValue);
						}

						try
                        {
							statWriteCommands++;
							statWriteDataBytes += dataLength * 2;

							await modbus.WriteShortRegistersAsync(new ModbusRequest(
								groupArr[0].Device.SlaveAddress,
								groupArr[startInd].Register.Number,
								(ushort)dataLength,
								firstRegister.RegType,
								groupArr[0].Device.ReadRetryCount,
								groupArr[0].Device.ReadTimeout,
								groupArr[0].Device.WriteTimeout
							), data);

							result = true;
						}
						catch (Exception ex)
                        {
							logger.LogError(ex, $"Ошибка записи в устройство");
							result = false;
						}
					}

					for (var regInd = startInd; regInd < endInd; regInd++)
					{
						var currReg = groupArr[regInd];
                        topickStateService.RemoveTopicState(currReg.Register.Name);
					}

					startInd = endInd;
                }
            }

            return result;

		}
    }
}
