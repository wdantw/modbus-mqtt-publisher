using MudbusMqttPublisher.Server.Contracts.Settings;
using NModbus;
using System.IO.Ports;
using NModbus.Serial;
using MudbusMqttPublisher.Server.Common;
using MudbusMqttPublisher.Server.Contracts;
using System.Text;
using Microsoft.Extensions.Logging;
using System.IO.Pipelines;
using System.Linq;
using System.Collections.Generic;

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
        private readonly IQueueRepository queueRepository;
        private readonly IModbusFactory modbusFactory;
        private readonly ITopicStateService topickStateService;
        private readonly IMqttPublisher mqttPublisher;

        Dictionary<byte, DateTime> deviceLastReadTimes = new();

        public ModbusQueueService(
            PortSettings settings,
            ILogger<ModbusQueueService> logger,
            IQueueRepository queueRepository,
            IModbusFactory modbusFactory,
            ITopicStateService topickStateService,
            IMqttPublisher mqttPublisher)
        {
            this.settings = settings;
            this.logger = logger;
            this.queueRepository = queueRepository;
            this.modbusFactory = modbusFactory;
            this.topickStateService = topickStateService;
            this.mqttPublisher = mqttPublisher;
        }

        private DateTime GetNextReadTime(DeviceSettings dev, RegisterSettings reg)
        {
            var result = (topickStateService.GetTopicState(reg.Name)?.ReadTime ?? DateTime.MinValue) + reg.ReadPeriod;

            if (deviceLastReadTimes.TryGetValue(dev.SlaveAddress, out var deviceLastReadTime))
            {
                var minDeviceNextReadTime = deviceLastReadTime + dev.MinSleepTimeout;
                if (result < minDeviceNextReadTime)
                    result = minDeviceNextReadTime;
            }

            return result;
        }

        private (DateTime? waitForTime, ReadTaskRequest? readTaskRequest) GetReadTask()
        {
            var currTime = DateTime.Now;

            var pending = settings.Devices.SelectMany(d => d.Registers.Select(r => new { Device = d, Register = r, NextReadTime = GetNextReadTime(d, r) }))
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
                .TakeWhileWithPrev((p, c) => {
                    var holeSize = p.Register.Number - c.Register.EndRegisterNumber;
                    if (p.Register.RegType.IsBitReg())
                    {
                        return holeSize <= first.Device.MaxBitHole;
                    }
                    else
                    {
                        return holeSize <= first.Device.MaxRegHole;
                    }
                })
                .ToArray();

            var after = pending
                .Where(r => r.Register.Number >= first.Register.Number)
                .OrderBy(r => r.Register.Number)
                .TakeWhileWithPrev((p, c) => {
                    var holeSize = c.Register.Number - p.Register.EndRegisterNumber;
                    if (p.Register.RegType.IsBitReg())
                    {
                        return holeSize <= first.Device.MaxBitHole;
                    }
                    else
                    {
                        return holeSize <= first.Device.MaxRegHole;
                    }
                })
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
            using var port = new SerialPort(settings.PortName);

            port.BaudRate = settings.BaudRate;
            port.DataBits = settings.DataBits;
            port.Parity = settings.Parity;
            port.StopBits = settings.StopBits;
            //port.DtrEnable = true;
            //port.RtsEnable = true;
            port.Open();

            using var master = modbusFactory.CreateRtuMaster(port);

            using var regHandle = queueRepository.RegisterQueue(this, settings.PortName);

            while (!cancellationToken.IsCancellationRequested)
            {
                (var waitForTime, var readTaskRequest) = GetReadTask();
                
                if (waitForTime.HasValue)
                {
                    var delay = waitForTime.Value - DateTime.Now;
                    logger.LogDebug($"Ожидание следующего чтения {delay}");
                    await Task.Delay(delay, cancellationToken);
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

                    var regValue = readResult[i];

                    foreach (var reg in registers)
                    {
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
                        var regValue = TypeConverter.Convert(readResult.Skip(i).Take(reg.SizeInRegisters).ToArray(), reg.RegFormat, reg.SizeInRegisters);

                        if (reg.Scale != null)
                        {
                            var doubleRegValue = Convert.ToDouble(regValue);
                            doubleRegValue = doubleRegValue * reg.Scale.Value;
                            if (reg.Precision.HasValue)
                            {
                                doubleRegValue = Math.Round(doubleRegValue, reg.Precision.Value);
                            }
                            regValue = doubleRegValue;
                        }

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

    }
}
