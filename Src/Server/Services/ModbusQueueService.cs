using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Services.Modbus;
using ModbusMqttPublisher.Server.Domain;
using System.Diagnostics.Metrics;
using ModbusMqttPublisher.Server.Services.Modbus.Enums;
using ModbusMqttPublisher.Server.Services.Modbus.Utils;
using ModbusMqttPublisher.Server.Services.Values;
using MQTTnet.DependencyInjection;
using MQTTnet;
using System.Buffers;

namespace ModbusMqttPublisher.Server.Services
{
    public class ModbusQueueService : IQueueService
    {
        private readonly ReadPort _settings;
        private readonly ILogger<ModbusQueueService> _logger;
        private readonly IModbusClientFactory _modbusFactory;
        private readonly IWriteQueueService _writeQueueService;
        private readonly IMqttPublisher _mqttPublisher;

        private readonly Counter<int> _readCallCounter;
        private readonly DiagnosticTimeCounter _readCallDurationCounter;
        private readonly Counter<int> _writeCallCounter;
        private readonly DiagnosticTimeCounter _writeCallDurationCounter;
        private readonly Counter<int> _wbEventCycleCounter;
        private readonly DiagnosticTimeCounter _wbEventCycleDurationCounter;

        private DateTime _nextReadWbEvents = DateTime.MinValue;

        public ModbusQueueService(
            ReadPort settings,
            ILogger<ModbusQueueService> logger,
            IModbusClientFactory modbusFactory,
            IWriteQueueService writeQueueService,
            IMeterFactory meterFactory,
            IMqttPublisher mqttPublisher)
        {
            _settings = settings;
            _logger = logger;
            _modbusFactory = modbusFactory;
            _writeQueueService = writeQueueService;
            _mqttPublisher = mqttPublisher;

            var meter = meterFactory.Create(GetType().FullName!, tags: new Dictionary<string, object?> { { "Serial", settings.SerialName } });
            _readCallCounter = meter.CreateCounter<int>("publisher.queue.read.calls", "calls", "Количество вызовов метода чтения регистров");
            _readCallDurationCounter = new DiagnosticTimeCounter(meter.CreateCounter<double>("publisher.queue.read.duration", "ms", "Время, проведенное в методах чтения регистров"));
            _writeCallCounter = meter.CreateCounter<int>("publisher.queue.write.calls", "calls", "Количество вызовов метода записи в регистры");
            _writeCallDurationCounter = new DiagnosticTimeCounter(meter.CreateCounter<double>("publisher.queue.write.duration", "ms", "Время, проведенное в методах записи в регистры"));
            _wbEventCycleCounter = meter.CreateCounter<int>("publisher.queue.wbevent.cycles", "calls", "Количество циклов опроса событий wirenboard");
            _wbEventCycleDurationCounter = new DiagnosticTimeCounter(meter.CreateCounter<double>("publisher.queue.wbevent.duration", "ms", "Время, проведенное в методе опроса событий wirenboard"));
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            await Task.Yield();

			using var modbusClient = _modbusFactory.Create(_settings);

			while (!cancellationToken.IsCancellationRequested)
            {
				_logger.LogTrace($"Новый цикл работы с портом");

                // если есть задачи на запись
				var writeQuery = _writeQueueService.GetQuery(_settings.SerialName);
                if (writeQuery != null)
                {
                    await PerfomWriteRequest(modbusClient, writeQuery, cancellationToken);
                    _writeQueueService.AcceptDequeued(_settings.SerialName);
                    continue;
                }

                // если необходимо сконфигурировать wbEvents
                if (_settings.AllowWbEvents)
                {
                    try
                    {
                        if (await PerfomReadWbEventsConfiguration(modbusClient, cancellationToken))
                            continue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка настройки wb событий");
                        continue;
                    }
                }

                var currTime = DateTime.Now;
                var nextReadTime = _settings.NextReadTime;

                if (_settings.AllowWbEvents && _nextReadWbEvents < nextReadTime)
                {
                    try
                    {
                        await PerfomReadWbEvents(modbusClient, cancellationToken);
                        _nextReadWbEvents = currTime;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка получения wb событий");
                    }
                    continue;
                }

                if (nextReadTime <= currTime)
                {
                    var readTask = _settings.GetNextReadTask(currTime);

                    if (readTask != null)
                        await PerfomReadRequest(modbusClient, readTask, cancellationToken);

                    continue;
                }

                var delay = nextReadTime - currTime;

                if (!_settings.AllowWbEvents && delay > TimeSpan.Zero)
                {
                    await await AsyncExtensions.WhenAnyCancellable(
                        ct => _writeQueueService.WaitForItems(_settings.SerialName, ct),
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
                    _logger.LogError(ex, "Ошибка чтения из Modbus");
					readTask.AccessFailed(DateTime.Now);
                    return false;
				}

                foreach(var reg in readTask.Registers)
                {
                    await ReadRegisterFromModbus(reg, readResult.GetSegment(reg.StartNumber - readTask.StartNumber, 1), readTime, cancellationToken);
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
					_logger.LogError(ex, "Ошибка чтения из Modbus");
                    readTask.AccessFailed(DateTime.Now);
                    return false;
				}

				foreach (var reg in readTask.Registers)
				{
                    await ReadRegisterFromModbus(reg, readResult.GetSegment(reg.StartNumber - readTask.StartNumber, reg.SizeInRegisters), readTime, cancellationToken);
                }
            }

            return true;
        }

        private async Task<bool> PerfomWriteRequest(IModbusClient modbus, WriteQuery writeQuery, CancellationToken cancellationToken)
        {
            _writeCallCounter.Add(1);
            using var _ = _writeCallDurationCounter.GetStartHolder();

            var writeTask = _settings.GetWriteTask(writeQuery.TopicName);

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
					register.IncomeValueConverter.ToModbus(writeQuery.Value.ToSpan(), data);
                    _logger.LogDebug("Запись в modbus {registerName}", register.Name);

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
					_logger.LogError(ex, $"Ошибка записи в устройство");

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
					register.IncomeValueConverter.ToModbus(writeQuery.Value.ToSpan(), data);
                    _logger.LogDebug("Запись в modbus {registerName}", register.Name);

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
					_logger.LogError(ex, $"Ошибка записи в устройство");
                    
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
            foreach (var dev in _settings.Devices)
            {
                if (!dev.NeedWbEventsConfigure)
                    continue;

                var configs = dev.GetWbEventConfigurations().ToArray();
                var readResult = await modbus.WbConfigureEvents(dev.SlaveAddress, configs, cancellationToken);
                _logger.LogInformation("Настройка wb событий для устройства {devAddress}", dev.SlaveAddress);
                dev.ApplyWbEventsConfiguration(readResult);
                result = true;
            }
            return result;
        }

        private async Task PerfomReadWbEvents(IModbusClient modbus, CancellationToken cancellationToken)
        {
            _wbEventCycleCounter.Add(1);
            using var _ = _wbEventCycleDurationCounter.GetStartHolder();

            var readTime = DateTime.Now;

            byte minSlaveAddress = _settings.MinSlaveAddress;
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
                {
                    _logger.LogDebug("Запрос событий wb не вернул ни одного события");

                    // возникает если поставить System-Rebooted событию приоритет Hi до его акцепта. пропустим эти события. акцептировать из бессмысленно - это не исправляет глюка
                    if (readResult.SlaveAddress >= _settings.MaxSlaveAddress)
                    {
                        break;
                    }
                    else
                    {
                        minSlaveAddress = (byte)(readResult.SlaveAddress + 1);
                        acceptSlaveAddress = readResult.SlaveAddress;
                        acceptFlag = readResult.AcceptFlag;
                        minSlaveAddressEventCountLimit = byte.MaxValue;
                        continue;
                    }
                }

                var device = _settings.GetDeice(readResult.SlaveAddress);

                if (device == null)
                {
                    _logger.LogWarning("Пришли Wb события от устройства, которе не указано в настройках {slaveAddress}", readResult.SlaveAddress);

                    // дальше зарегистрированных устройств точно не будет
                    if (readResult.SlaveAddress >= _settings.MaxSlaveAddress)
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

                    if (readResult.SlaveAddress >= _settings.MaxSlaveAddress)
                    {
                        // дальше зарегистрированных устройств точно не будет
                        // но нужно акцептировать текущие события. если последнее устройство спамит, то получим бесконечный цикл. используем lastDeviceReadFinished флаг для предотвращения

                        if (lastDeviceReadFinished)
                        {
                            // в прошлый цикл запроса уже устновили флаг lastDeviceReadFinished но сигнала о том, что событий больше нет не получили. прерываем цикл запроса принудительно
                            // без потверждения событий
                            _logger.LogDebug("Сработало предотвращение спама последнего устройства");
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
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        var dataStr = wbEvent.EventData != null ? BitConverter.ToString(wbEvent.EventData) : "null";
                        _logger.LogDebug("Получено событие SlaveAddress:{SlaveAddress} Type:{type} id:{id} data:{data} EventCount:{EventCount}", readResult.SlaveAddress, wbEvent.EventType, wbEvent.EventId, dataStr, readResult.EventCount);
                    }

                    switch (wbEvent.EventType)
                    {
                        case WBEventType.System:
                            {
                                if (wbEvent.EventId == (ushort)WbSystemEventId.Rebooted)
                                {
                                    if (wbEvent.EventData?.Length > 0)
                                        _logger.LogWarning("От устройства {slaveAddress} поступило System событие Rebooted к которому привязаны данные", readResult.SlaveAddress);

                                    _logger.LogInformation("Уствройство перезагружено {devAddress}", device.SlaveAddress);
                                    device.DeviceRebooted();
                                }
                                else
                                {
                                    _logger.LogWarning("От устройства {slaveAddress} поступило неизвестное System событие с идентификатором {EventId}", readResult.SlaveAddress, wbEvent.EventId);
                                }
                            }
                            break;
                        case WBEventType.Coil:
                            {
                                var register = device.GetRegisterByAddress(RegisterType.Coil, wbEvent.EventId);
                                if (register != null)
                                {
                                    if (wbEvent.EventData?.Length != 1)
                                        throw new Exception("Количество данных для регистра Coil ожидалось - 1 байт");

                                    await ReadRegisterFromModbus(register, new bool[] { wbEvent.EventData[0] != 0 }, readTime, cancellationToken);
                                }
                                else
                                {
                                    _logger.LogWarning("От устройства {slaveAddress} поступило событие Coil для не зарегистрированного регистра {EventId}", readResult.SlaveAddress, wbEvent.EventId);
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

                                    await ReadRegisterFromModbus(register, new bool[] { wbEvent.EventData[0] != 0 }, readTime, cancellationToken);
                                }
                                else
                                {
                                    _logger.LogWarning("От устройства {slaveAddress} поступило событие DiscreteInput для не зарегистрированного регистра {EventId}", readResult.SlaveAddress, wbEvent.EventId);
                                }
                            }
                            break;
                        case WBEventType.Holding:
                            {
                                var register = device.GetRegisterByAddress(RegisterType.HoldingRegister, wbEvent.EventId);
                                if (register != null)
                                {
                                    if (wbEvent.EventData == null || wbEvent.EventData.Length == 0 || wbEvent.EventData.Length % 2 != 0)
                                        throw new Exception($"Количество данных для регистра Holding больше нуля байт и кратно двум, но получено {wbEvent.EventData?.Length ?? 0}");

                                    var modbusData = new ushort[wbEvent.EventData.Length / 2];
                                    for(int i = 0; i < wbEvent.EventData.Length / 2; i++)
                                        modbusData[modbusData.Length - 1 - i] = ByteOrderUtils.ToUInt16LE(wbEvent.EventData.AsSpan().Slice(i * 2, 2));

                                    await ReadRegisterFromModbus(register, modbusData, readTime, cancellationToken);
                                }
                                else
                                {
                                    _logger.LogWarning("От устройства {slaveAddress} поступило событие Holding для не зарегистрированного регистра {EventId}", readResult.SlaveAddress, wbEvent.EventId);
                                }
                            }
                            break;
                        case WBEventType.Input:
                            {
                                var register = device.GetRegisterByAddress(RegisterType.InputRegister, wbEvent.EventId);
                                if (register != null)
                                {
                                    if (wbEvent.EventData == null || wbEvent.EventData.Length == 0 || wbEvent.EventData.Length % 2 != 0)
                                        throw new Exception($"Количество данных для регистра Input больше нуля байт и кратно двум, но получено {wbEvent.EventData?.Length ?? 0}");

                                    var modbusData = new ushort[wbEvent.EventData.Length / 2];
                                    for (int i = 0; i < wbEvent.EventData.Length / 2; i++)
                                        modbusData[modbusData.Length - 1 - i] = ByteOrderUtils.ToUInt16LE(wbEvent.EventData.AsSpan().Slice(i * 2, 2));

                                    await ReadRegisterFromModbus(register, modbusData, readTime, cancellationToken);
                                }
                                else
                                {
                                    _logger.LogWarning("От устройства {slaveAddress} поступило событие Input для не зарегистрированного регистра {EventId}", readResult.SlaveAddress, wbEvent.EventId);
                                }
                            }
                            break;
                    }
                }
            }
        }

        private async Task ReadRegisterFromModbus(ReadRegister register, ArraySegment<ushort> modbusData, DateTime readTime, CancellationToken cancellationToken)
        {
            var needPublish = register.ReadFromModbus(readTime, modbusData);
            if (needPublish)
                await PublishRegister(register, cancellationToken);
        }

        private async Task ReadRegisterFromModbus(ReadRegister register, ArraySegment<bool> modbusData, DateTime readTime, CancellationToken cancellationToken)
        {
            var needPublish = register.ReadFromModbus(readTime, modbusData);
            if (needPublish)
                await PublishRegister(register, cancellationToken);
        }

        private async Task PublishRegister(ReadRegister register, CancellationToken cancellationToken)
        {
            await PublishValue(register.Name, register.PublishValue, cancellationToken);
        }

        private async Task PublishValue(string name, IPublishValueSorage value, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Для регистра {regName} обновлены данные {value}", name, value);

            var applicationMessage = new MqttApplicationMessageBuilder()
                   .WithTopic(name)
                   .WithPayload(value.ToMqtt())
                   .WithRetainFlag(true)
                   .Build();

            await _mqttPublisher.PublishAsync(applicationMessage, cancellationToken);
        }
    }
}
