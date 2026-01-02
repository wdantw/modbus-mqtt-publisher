using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Services.Modbus;
using ModbusMqttPublisher.Server.Services.Values;
using ModbusMqttPublisher.Server.Services.Mqtt;
using ModbusMqttPublisher.Server.Domain;
using System.Diagnostics.Metrics;
using ModbusMqttPublisher.Server.Services.Modbus.Enums;

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

        private DateTime _nextReadWbEvents = DateTime.MinValue;

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
        }

		public async Task Run(CancellationToken cancellationToken)
        {
            await Task.Yield();

			using var modbusClient = modbusFactory.Create(settings);

			while (!cancellationToken.IsCancellationRequested)
            {
				logger.LogTrace($"Новый цикл работы с портом");

                // если есть задачи на запись
				var writeQuery = writeQueueService.GetQuery(settings.SerialName);
                if (writeQuery != null)
                {
                    await PerfomWriteRequest(modbusClient, writeQuery, cancellationToken);
                    writeQueueService.AcceptDequeued(settings.SerialName);
                    continue;
                }

                // если необходимо сконфигурировать wbEvents
                if (settings.AllowWbEvents)
                {
                    try
                    {
                        if (await PerfomReadWbEventsConfiguration(modbusClient, cancellationToken))
                            continue;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка настройки wb событий");
                        continue;
                    }
                }

                var currTime = DateTime.Now;
                var nextReadTime = settings.NextReadTime;

                if (settings.AllowWbEvents && _nextReadWbEvents < nextReadTime)
                {
                    try
                    {
                        await PerfomReadWbEvents(modbusClient, cancellationToken);
                        _nextReadWbEvents = nextReadTime;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка получения wb событий");
                    }
                    continue;
                }

                if (nextReadTime <= currTime)
                {
                    var readTask = settings.GetNextReadTask(currTime);

                    if (readTask != null)
                        await PerfomReadRequest(modbusClient, readTask, cancellationToken);

                    continue;
                }

                var delay = nextReadTime - currTime;

                if (!settings.AllowWbEvents && delay > TimeSpan.Zero)
                {
                    await await AsyncExtensions.WhenAnyCancellable(
                        ct => writeQueueService.WaitForItems(settings.SerialName, ct),
                        ct => Task.Delay(delay, ct),
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
					readResult = await modbus.ReadBitRegistersAsync(
                        new ModbusRequest(
                            SlaveAddress: readTask.Device.SlaveAddress,
                            StartRegister: readTask.StartNumber,
                            RegisterCount: readTask.RegisterCount,
                            RegisterType: readTask.RegisterType),
                        cancellationToken);
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
					readResult = await modbus.ReadShortRegistersAsync(
                        new ModbusRequest(
                            SlaveAddress: readTask.Device.SlaveAddress,
                            StartRegister: readTask.StartNumber,
                            RegisterCount: readTask.RegisterCount,
                            RegisterType: readTask.RegisterType),
                        cancellationToken);
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

        private async Task<bool> PerfomWriteRequest(IModbusClient modbus, WriteQuery writeQuery, CancellationToken cancellationToken)
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

					await modbus.WriteBitRegistersAsync(
                        new ModbusRequest(
                            SlaveAddress: device.SlaveAddress,
                            StartRegister: register.StartNumber,
                            RegisterCount: (ushort)dataLength,
                            RegisterType: register.RegisterType),
                        data,
                        cancellationToken);

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
					await modbus.WriteShortRegistersAsync(
                        new ModbusRequest(
                            SlaveAddress: device.SlaveAddress,
                            StartRegister: register.StartNumber,
                            RegisterCount: (ushort)dataLength,
                            RegisterType: register.RegisterType),
                        data,
                        cancellationToken);

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

        private async Task<bool> PerfomReadWbEventsConfiguration(IModbusClient modbus, CancellationToken cancellationToken)
        {
            var result = false;
            foreach (var dev in settings.Devices)
            {
                if (!dev.NeedWbEventsConfigure)
                    continue;

                var configs = dev.GetWbEventConfigurations().ToArray();
                var readResult = await modbus.WbConfigureEvents(dev.SlaveAddress, configs, cancellationToken);
                logger.LogInformation("Настройка wb событий для устройства {devAddress}", dev.SlaveAddress);
                dev.ApplyWbEventsConfiguration(readResult);
                result = true;
            }
            return result;
        }

        private async Task PerfomReadWbEvents(IModbusClient modbus, CancellationToken cancellationToken)
        {
            var readTime = DateTime.Now;

            byte minSlaveAddress = settings.MinSlaveAddress;
            byte acceptSlaveAddress = 0;
            byte acceptFlag = 0;
            // устанавливаем лимит событий, чтоб устройства не спамили
            int minSlaveAddressEventCountLimit = byte.MaxValue;
            bool lastDeviceReadFinished = false;

            while (true)
            {
                var readResult = await modbus.WbRequestEventsAsync(minSlaveAddress: minSlaveAddress, acceptEventsSlaveAddress: acceptSlaveAddress, acceptEventsFlag: acceptFlag, cancellationToken);

                // события закончились
                if (readResult == null)
                    break;

                if (readResult.Events == null || readResult.Events.Length == 0)
                    throw new Exception("Запрос событий wb не вернул ни одного события");

                var device = settings.GetDeice(readResult.SlaveAddress);

                if (device == null)
                {
                    logger.LogWarning("Пришли Wb события от устройства, которе не указано в настройках {slaveAddress}", readResult.SlaveAddress);

                    // дальше зарегистрированных устройств точно не будет
                    if (readResult.SlaveAddress >= settings.MaxSlaveAddress)
                        break;

                    minSlaveAddress = (byte)(readResult.SlaveAddress + 1);
                    acceptSlaveAddress = 0;
                    acceptFlag = 0;
                    minSlaveAddressEventCountLimit = byte.MaxValue;
                    continue;
                }

                // вычисляем ограничения по количеству событий и параметры следующего запроса
                // логика в том, что от события получем не больше 256 событий за цикл опроса и не больше чем получено в EventCount в первом ответе устройства
                // обработка событий после вычислений нужна для того, что бы не обрабатывать события, которые мы потом не подвердим

                if (minSlaveAddress != readResult.SlaveAddress)
                    minSlaveAddressEventCountLimit = byte.MaxValue;

                if (minSlaveAddressEventCountLimit > readResult.EventCount)
                    minSlaveAddressEventCountLimit = readResult.EventCount;

                minSlaveAddressEventCountLimit -= readResult.Events.Length;

                if (minSlaveAddressEventCountLimit > 0)
                {
                    // можем еще получить событий от этого устройства
                    
                    minSlaveAddress = readResult.SlaveAddress;
                    acceptSlaveAddress = readResult.SlaveAddress;
                    acceptFlag = readResult.AcceptFlag;
                }
                else
                {
                    // принудительно переключаемся на следующее устройство

                    if (readResult.SlaveAddress >= settings.MaxSlaveAddress)
                    {
                        // дальше зарегистрированных устройств точно не будет
                        // но нужно акцептировать текущие события. если последнее устройство спамит, то получим бесконечный цикл. используем lastDeviceReadFinished флаг для предотвращения

                        if (lastDeviceReadFinished)
                        {
                            // в прошлый цикл запроса уже устновили флаг lastDeviceReadFinished но сигнала о том, что событий больше нет не получили. прерываем цикл запроса принудительно
                            // без потверждения событий
                            break;
                        }

                        minSlaveAddress = readResult.SlaveAddress;
                        acceptSlaveAddress = readResult.SlaveAddress;
                        acceptFlag = readResult.AcceptFlag;
                        minSlaveAddressEventCountLimit = 0;
                        lastDeviceReadFinished = true;
                    }
                    else
                    {
                        minSlaveAddress = (byte)(readResult.SlaveAddress + 1);
                        acceptSlaveAddress = readResult.SlaveAddress;
                        acceptFlag = readResult.AcceptFlag;
                        minSlaveAddressEventCountLimit = byte.MaxValue;
                    }
                }

                // обработка событий readResult.Events для устройства device
                foreach (var wbEvent in readResult.Events)
                {
                    logger.LogInformation("Получено событие {eventType} от {devAddress} регистр: {regNum}", wbEvent.EventType, device.SlaveAddress, wbEvent.EventId);

                    switch (wbEvent.EventType)
                    {
                        case WBEventType.System:
                            {
                                if (wbEvent.EventId == (ushort)WbSystemEventId.Rebooted)
                                {
                                    if (wbEvent.EventData?.Length > 0)
                                        logger.LogWarning("От устройства {slaveAddress} поступило System событие Rebooted к которому привязаны данные", readResult.SlaveAddress);

                                    logger.LogInformation("Уствройство перезагружено {devAddress}", device.SlaveAddress);
                                    device.DeviceRebooted();
                                }
                                else
                                {
                                    logger.LogWarning("От устройства {slaveAddress} поступило неизвестное System событие с идентификатором {EventId}", readResult.SlaveAddress, wbEvent.EventId);
                                }
                            }
                            break;
                        case WBEventType.Colil:
                            {
                                var register = device.GetRegisterByAddress(RegisterType.Coil, wbEvent.EventId);
                                if (register != null)
                                {
                                    if (wbEvent.EventData?.Length != 1)
                                        throw new Exception("Количество данных для регистра Coil ожидалось - 1 байт");

                                    var needPublish = register.ReadFromModbus(readTime, new bool[] { wbEvent.EventData[0] != 0 });

                                    logger.LogDebug("Для регистра {regName} получены данные {value}", register.Name, register.PublishValue);

                                    if (needPublish)
                                    {
                                        await mqttBus.EnqueueMessage(register.Name, register.PublishValue.ToMqtt(), true, cancellationToken);
                                    }
                                }
                                else
                                {
                                    logger.LogWarning("От устройства {slaveAddress} поступило событие Colil для не зарегистрированного регистра {EventId}", readResult.SlaveAddress, wbEvent.EventId);
                                }
                            }
                            break;
                        case WBEventType.Discrete:
                            {
                                var register = device.GetRegisterByAddress(RegisterType.DiscreteInput, wbEvent.EventId);
                                if (register != null)
                                {
                                    if (wbEvent.EventData?.Length != 1)
                                        throw new Exception("Количество данных для регистра DiscreteInput ожидалось - 1 байт");

                                    var needPublish = register.ReadFromModbus(readTime, new bool[] { wbEvent.EventData[0] != 0 });

                                    logger.LogDebug("Для регистра {regName} получены данные {value}", register.Name, register.PublishValue);

                                    if (needPublish)
                                    {
                                        await mqttBus.EnqueueMessage(register.Name, register.PublishValue.ToMqtt(), true, cancellationToken);
                                    }
                                }
                                else
                                {
                                    logger.LogWarning("От устройства {slaveAddress} поступило событие DiscreteInput для не зарегистрированного регистра {EventId}", readResult.SlaveAddress, wbEvent.EventId);
                                }
                            }
                            break;
                        case WBEventType.Holding:
                            break;
                        case WBEventType.Input:
                            break;
                    }
                }
            }
        }
    }
}
