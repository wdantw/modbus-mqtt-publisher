using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Services.Modbus;
using ModbusMqttPublisher.Server.Services.Values;
using ModbusMqttPublisher.Server.Services.Mqtt;
using ModbusMqttPublisher.Server.Domain;
using System.Diagnostics.Metrics;

namespace ModbusMqttPublisher.Server.Services
{
    public class ModbusQueueService : IQueueService
    {
        private readonly ReadPort settings;
        private readonly ILogger<ModbusQueueService> logger;
        private readonly IModbusClientFactory modbusFactory;
        private readonly IMqttBus mqttBus;
        private readonly IWriteQueueService writeQueueService;

        private readonly Counter<int> _readCallCounter;
        private readonly DiagnosticTimeCounter _readCallDurationCounter;
        private readonly Counter<int> _writeCallCounter;
        private readonly DiagnosticTimeCounter _writeCallDurationCounter;
        private readonly DiagnosticTimeCounter _sleepCallDurationCounter;

        public ModbusQueueService(
            ReadPort settings,
            ILogger<ModbusQueueService> logger,
            IModbusClientFactory modbusFactory,
            IMqttBus mqttBus,
            IWriteQueueService writeQueueService,
            IMeterFactory meterFactory)
        {
            this.settings = settings;
            this.logger = logger;
            this.modbusFactory = modbusFactory;
            this.mqttBus = mqttBus;
            this.writeQueueService = writeQueueService;

            var meter = meterFactory.Create(GetType().FullName!, tags: new Dictionary<string, object?> { { "Serial", settings.SerialName } });
            _readCallCounter = meter.CreateCounter<int>("publisher.queue.read.calls", "calls", "Количество вызовов метода чтения регистров");
            _readCallDurationCounter = new DiagnosticTimeCounter(meter.CreateCounter<double>("publisher.queue.read.duration", "ms", "Время, проведенное в методах чтения регистров"));
            _writeCallCounter = meter.CreateCounter<int>("publisher.queue.write.calls", "calls", "Количество вызовов метода записи в регистры");
            _writeCallDurationCounter = new DiagnosticTimeCounter(meter.CreateCounter<double>("publisher.queue.write.duration", "ms", "Время, проведенное в методах записи в регистры"));
            _sleepCallDurationCounter = new DiagnosticTimeCounter(meter.CreateCounter<double>("publisher.queue.sleep.duration", "ms", "Время, проведенное в принудительном ожидании"));
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

		public async Task Run(CancellationToken cancellationToken)
        {
            await Task.Yield();

			using var modbusClient = modbusFactory.Create(settings);

			while (!cancellationToken.IsCancellationRequested)
            {
				logger.LogDebug($"Новый цикл работы с портом");

				await modbusClient.CheckConnection(settings.ErrorSleepTimeout, cancellationToken);

				var writeQuery = writeQueueService.GetQuery(settings.SerialName);
                if (writeQuery != null)
                {
                    await PerfomWriteRequest(modbusClient, writeQuery);
                    writeQueueService.AcceptDequeued(settings.SerialName);
					await PerfomSleep(settings.MinSleepTimeout, cancellationToken);
                    continue;
                }

				var currTime = DateTime.Now;
                var nextReadTime = settings.NextReadTime;

                if (nextReadTime <= currTime)
                {
                    var readTask = settings.GetNextReadTask(currTime);

                    if (readTask != null)
                    {
                        await PerfomReadRequest(modbusClient, readTask, cancellationToken);
                        await PerfomSleep(settings.MinSleepTimeout, cancellationToken);
                    }

                    continue;
                }

                var delay = nextReadTime - currTime;
				
                if (delay > TimeSpan.Zero)
                {
					logger.LogDebug("Ожидание следующего чтения {delay}", delay);

					await await AsyncExtensions.WhenAnyCancellable(
						ct => writeQueueService.WaitForItems(settings.SerialName, ct),
						ct => PerfomSleep(delay, ct),
						cancellationToken);
				}
            }
        }

        private async Task<bool> PerfomReadRequest(IModbusClient modbus, ReadTask readTask, CancellationToken cancellationToken)
        {
            _readCallCounter.Add(1);
            using var _ = _readCallDurationCounter.GetStartHolder();

            var readTime = DateTime.Now;

            if (readTask.RegisterType.IsBitReg())
            {
                bool[] readResult;

                try
                {
					readResult = await modbus.ReadBitRegistersAsync(new ModbusRequest(
						readTask.Device.SlaveAddress,
						readTask.StartNumber,
						readTask.RegisterCount,
                        readTask.RegisterType,
                        readTask.Device.ReadRetryCount,
                        readTask.Device.ReadTimeout,
                        readTask.Device.WriteTimeout
					));
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
					readResult = await modbus.ReadShortRegistersAsync(new ModbusRequest(
						readTask.Device.SlaveAddress,
						readTask.StartNumber,
						readTask.RegisterCount,
						readTask.RegisterType,
						readTask.Device.ReadRetryCount,
						readTask.Device.ReadTimeout,
						readTask.Device.WriteTimeout
					));
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
            _writeCallCounter.Add(1);
            using var _ = _writeCallDurationCounter.GetStartHolder();

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
    
		private async Task PerfomSleep(TimeSpan sleepTime, CancellationToken cancellationToken)
		{
			using var _ = _sleepCallDurationCounter.GetStartHolder();
			await Task.Delay(sleepTime, cancellationToken);
        }
	}
}
