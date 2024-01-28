using MudbusMqttPublisher.Server.Contracts.Settings;
using NModbus;
using System.IO.Ports;
using NModbus.Serial;
using MudbusMqttPublisher.Server.Common;
using MudbusMqttPublisher.Server.Contracts;
using MudbusMqttPublisher.Server.Services.Types;

namespace MudbusMqttPublisher.Server.Services
{
	public class ModbusQueueService : IQueueService
    {
        public record ReadTaskRequest
        (
            DeviceSettings Device,
            RegisterType RegType,
            int StartRegister,
            int RegisterCount
        );

        private readonly PortSettings settings;
        private readonly ILogger<ModbusQueueService> logger;
        private readonly IModbusFactory modbusFactory;
        private readonly ITopicStateService topickStateService;
        private readonly IMqttPublisher mqttPublisher;
        private readonly IWriteQueueService writeQueueService;
        private readonly IRegisterValueFactory registerValueFactory;

		Dictionary<byte, DateTime> deviceLastReadTimes = new();

		public ModbusQueueService(
			PortSettings settings,
			ILogger<ModbusQueueService> logger,
			IModbusFactory modbusFactory,
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
		}

		private DateTime GetNextReadTime(DeviceSettings dev, RegisterSettings reg, DateTime curTime)
        {
            DateTime result;

            var lastReadTime = topickStateService.GetTopicState(reg.Name)?.ReadTime;

            if (lastReadTime.HasValue)
            {
                if (reg.ReadPeriod.HasValue)
                    result = lastReadTime.Value + reg.ReadPeriod.Value;
                else
                    result = DateTime.MaxValue;
            }
            else
            {
                result = DateTime.MinValue;
            }

            if (deviceLastReadTimes.TryGetValue(dev.SlaveAddress, out var deviceLastReadTime))
            {
                var minDeviceNextReadTime = deviceLastReadTime + dev.MinSleepTimeout;
                // нелзя сравнивать с result, так как будет потерян приоритет для регистров, которые ни разу не читались
                if (curTime < minDeviceNextReadTime)
                    result = minDeviceNextReadTime;
            }

            return result;
        }

        private bool TestRegHole(RegisterSettings regLow, RegisterSettings regHi, DeviceSettings device)
        {
            var holeSize = regLow.Number - regHi.EndRegisterNumber;
            
            if (regLow.RegType.IsBitReg())
            {
                return holeSize <= device.MaxBitHole;
            }
            else
            {
                return holeSize <= device.MaxRegHole;
            }
        }

        private (DateTime? waitForTime, ReadTaskRequest? readTaskRequest) GetReadTask()
        {
            var currTime = DateTime.Now;

            var pending = settings.Devices.SelectMany(d => d.Registers.Select(r => new { Device = d, Register = r, NextReadTime = GetNextReadTime(d, r, currTime) }))
                .OrderBy(r => r.NextReadTime)
                .FirstAndFilterdByFirst((f, c) =>
                {
                    if (c.Device.SlaveAddress != f.Device.SlaveAddress) return false;
                    if (c.Register.RegType != f.Register.RegType) return false;
                    if (c.NextReadTime > currTime) return false;

                    var distace = Math.Max(c.Register.EndRegisterNumber, f.Register.EndRegisterNumber) - Math.Min(c.Register.Number, f.Register.Number);

                    if (f.Register.RegType.IsBitReg())
                    {
                        if (distace > f.Device.MaxReadBit) return false;
                    }
                    else
                    {
                        if (distace > f.Device.MaxReadRegisters) return false;
                    }
                    return true;
                })
                // если first не дождался совего времени, то выбираем только его, что бы исключить дальнейший анализ
                .TakeOnlyFirstIf(f => f.NextReadTime > currTime)
                .ToArray();

            if (pending.Length == 0)
            {
                return (DateTime.MaxValue, null);
            }

            var first = pending[0];

            if (first.NextReadTime > currTime)
            {
                return (first.NextReadTime, null);
            }

            // сейчас pending может содержать диапазон ячеек в два раза больше допустимого и не учитывает максимально длопустимое количество "дырок" в чтении

            var before = pending
                .Where(r => r.Register.Number <= first.Register.Number)
                .OrderByDescending(r => r.Register.Number)
                .TakeWhileWithPrev((p, c) => TestRegHole(p.Register, c.Register, first.Device))
                .ToArray();

            var after = pending
                .Where(r => r.Register.Number >= first.Register.Number)
                .OrderBy(r => r.Register.Number)
                .TakeWhileWithPrev((p, c) => TestRegHole(c.Register, p.Register, first.Device))
                .ToArray();

            var startNumber = first.Register.Number;
            var endNumber = first.Register.EndRegisterNumber;
            var indBefore = 1;
            var indAfter = 1;
            var maxRead = first.Register.RegType.IsBitReg() ? first.Device.MaxReadBit : first.Device.MaxReadRegisters;

            while (true)
            {
                var afterItem = indAfter < after.Length ? after[indAfter] : null;
                var beforeItem = indBefore < before.Length ? before[indBefore] : null;
                bool allowAfter = afterItem != null && (afterItem.Register.EndRegisterNumber - startNumber <= maxRead);
                bool allowBefore = beforeItem != null && (endNumber - beforeItem.Register.Number <= maxRead);

                if (!allowBefore && !allowAfter)
                    break;

                if (allowAfter && allowBefore)
                {
                    if (afterItem!.NextReadTime < beforeItem!.NextReadTime)
                    {
                        allowBefore = false;
                    }
                    else
                    {
                        allowAfter = false;
                    }
                }

                if (allowAfter)
                {
                    indAfter++;
                    endNumber = afterItem!.Register.EndRegisterNumber;
                }
                else
                {
                    indBefore++;
                    startNumber = beforeItem!.Register.Number;
                }
            }

            return (null, new ReadTaskRequest(first.Device, first.Register.RegType, startNumber, endNumber - startNumber));
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            using var port = new SerialPort(settings.SerialName);

            port.BaudRate = settings.BaudRate;
            port.DataBits = settings.DataBits;
            port.Parity = settings.Parity;
            port.StopBits = settings.StopBits;
			//port.DtrEnable = true;
			//port.RtsEnable = true;
			port.ReadTimeout = (int)settings.ReadTimeout.TotalMilliseconds;
			port.WriteTimeout = (int)settings.WriteTimeout.TotalMilliseconds;
			port.Open();

            using var master = modbusFactory.CreateRtuMaster(port);


			while (!cancellationToken.IsCancellationRequested)
            {
				logger.LogDebug($"Новый цикл работы с портом");

				var writeQueries = writeQueueService.GetQueries(settings.SerialName);
                if (writeQueries.Length > 0)
                {
                    await PerfomWriteRequest(master, writeQueries);
                    writeQueueService.AcceptDequeued(settings.SerialName);
                    await Task.Delay(settings.MinSleepTimeout, cancellationToken);
                    continue;
                }

                (var waitForTime, var readTaskRequest) = GetReadTask();
                
                if (waitForTime.HasValue)
                {
                    var delay = waitForTime.Value - DateTime.Now;
                    if (delay > TimeSpan.FromHours(1)) delay = TimeSpan.FromHours(1);
                    // чтоб не ломалось во время отладки или лагов
                    if (delay < TimeSpan.Zero) continue;
                    logger.LogDebug($"Ожидание следующего чтения {delay}");

                    await Task.WhenAny(writeQueueService.WaitForItems(settings.SerialName, cancellationToken), Task.Delay(delay));
                    continue;
                }

                await PerfomReadRequest(master, readTaskRequest!);
                await Task.Delay(settings.MinSleepTimeout, cancellationToken);
            }
        }

        private async Task PerfomReadRequest(IModbusMaster modbus, ReadTaskRequest readTask)
        {
            logger.LogDebug("Запрошено чтение информации из Modbus");
            var slaveAddress = readTask.Device.SlaveAddress;
            var startReg = (ushort)readTask.StartRegister;
            var regCount = (ushort)readTask.RegisterCount;
            var readTime = DateTime.Now;

            if (readTask.RegType.IsBitReg())
            {
                bool[] readResult;

                switch (readTask.RegType)
                {
                    case RegisterType.Coil:
                        readResult = await modbus.ReadCoilsAsync(slaveAddress, startReg, regCount);
                        break;
                    case RegisterType.DiscreteInput:
                        readResult = await modbus.ReadInputsAsync(slaveAddress, startReg, regCount);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                deviceLastReadTimes[slaveAddress] = DateTime.Now;

                for (int i = 0; i < readTask.RegisterCount; i++)
                {
                    int currNumber = readTask.StartRegister + i;
                    var registers = readTask.Device.Registers
                        .Where(r => r.Number == currNumber &&
                                    r.RegType == readTask.RegType &&
                                    r.RegFormat == RegisterFormat.Default);

                    foreach (var reg in registers)
                    {
                        var regValue = registerValueFactory.Create(reg);
                        regValue.FromModbus(readResult.GetSegment(i, 1));

						logger.LogDebug($"Для регистра {reg.Name} получены данные {regValue}");
                        if (topickStateService.UpdateTopicState(new TopickStateDto(reg.Name, regValue, readTime)))
                        {
                            logger.LogDebug($"Для регистра {reg.Name} данные добавлены в очередь отправки");
                            mqttPublisher.PublishTopic(reg.Name);
                        }
                    }
                }
            }
            else
            {
                ushort[] readResult;
                switch (readTask.RegType)
                {
                    case RegisterType.HoldingRegister:
                        readResult = await modbus.ReadHoldingRegistersAsync(slaveAddress, startReg, regCount);
                        break;
                    case RegisterType.InputRegister:
                        readResult = await modbus.ReadInputRegistersAsync(slaveAddress, startReg, regCount);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                deviceLastReadTimes[slaveAddress] = DateTime.Now;

                int endRegister = readTask.StartRegister + readTask.RegisterCount;

                for (int i = 0; i < readTask.RegisterCount; i++)
                {
                    int currNumber = readTask.StartRegister + i;
                    var registers = readTask.Device.Registers
                        .Where(r => r.Number == currNumber &&
                                    r.RegType == readTask.RegType &&
                                    r.EndRegisterNumber <= endRegister);

                    foreach (var reg in registers)
                    {
                        var regValue = registerValueFactory.Create(reg);
						regValue.FromModbus(readResult.GetSegment(i, reg.SizeInRegisters));

                        logger.LogDebug($"Для регистра {reg.Name} получены данные {regValue}");
                        if (topickStateService.UpdateTopicState(new TopickStateDto(reg.Name, regValue, readTime)))
                        {
                            logger.LogDebug($"Для регистра {reg.Name} данные добавлены в очередь отправки");
                            mqttPublisher.PublishTopic(reg.Name);
                        }
                    }
                }
            }
        }

        private async Task PerfomWriteRequest(IModbusMaster modbus, WriteQuery[] queries)
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

						if (firstRegister.RegType != RegisterType.Coil)
						{
							logger.LogError($"Нельзя писать в регистр {firstRegister.RegType}");
                            continue;
						}

						var dataLength = groupArr[endInd - 1].Register.EndRegisterNumber - firstRegister.Number;
						var data = new bool[dataLength];

						for (var regInd = startInd; regInd < endInd; regInd++)
						{
							var currReg = groupArr[regInd];
							currReg.Value.ToModbus(data.GetSegment(currReg.Register.Number - firstRegister.Number, currReg.Register.SizeInRegisters));
                            logger.LogDebug($"Запись в modbus {currReg.Register.Name} = {currReg.Value}");
						}

                        await modbus.WriteMultipleCoilsAsync(groupArr[0].Device.SlaveAddress, groupArr[startInd].Register.Number, data);
                    }
                    else
                    {
                        var firstRegister = groupArr[startInd].Register;

						if (firstRegister.RegType != RegisterType.HoldingRegister)
						{
							logger.LogError($"Нельзя писать в регистр {firstRegister.RegType}");
                            continue;
						}

						var dataLength = groupArr[endInd - 1].Register.EndRegisterNumber - firstRegister.Number;
                        var data = new ushort[dataLength];

                        for (var regInd = startInd; regInd < endInd; regInd++)
                        {
                            var currReg = groupArr[regInd];
                            currReg.Value.ToModbus(data.GetSegment(currReg.Register.Number - firstRegister.Number, currReg.Register.SizeInRegisters));
							logger.LogDebug($"Запись в modbus {currReg.Register.Name} = {currReg.Value}");
						}

                        await modbus.WriteMultipleRegistersAsync(groupArr[0].Device.SlaveAddress, groupArr[startInd].Register.Number, data);
                    }

					for (var regInd = startInd; regInd < endInd; regInd++)
					{
						var currReg = groupArr[regInd];
                        topickStateService.RemoveTopicState(currReg.Register.Name);
					}

					startInd = endInd;
                }
            }
        }
    }
}
