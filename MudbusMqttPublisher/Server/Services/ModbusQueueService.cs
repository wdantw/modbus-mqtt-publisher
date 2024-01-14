using MudbusMqttPublisher.Server.Contracts.Settings;
using NModbus;
using System.IO.Ports;
using NModbus.Serial;
using MudbusMqttPublisher.Server.Common;
using MudbusMqttPublisher.Server.Contracts;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace MudbusMqttPublisher.Server.Services
{
    public class ModbusQueueService : IQueueService
    {
        private class LastReadInfo
        {
            public string DeviceName { get; set; } = string.Empty;
            public int RegisterNumber { get; set; }
            public RegisterType RegType { get; set; }
            public DateTime LastReadTime { get; set; }
        }

        private readonly PortSettings settings;
        private readonly ILogger logger;
        private readonly IQueueRepository queueRepository;
        private readonly IModbusFactory modbusFactory;

        private List<LastReadInfo> lastReadInfos = new List<LastReadInfo>();


        public ModbusQueueService(PortSettings settings, ILogger logger, IQueueRepository queueRepository, IModbusFactory modbusFactory)
        {
            this.settings = settings;
            this.logger = logger;
            this.queueRepository = queueRepository;
            this.modbusFactory = modbusFactory;
        }

        private LastReadInfo? GetLastReadInfo(DeviceSettings dev, RegisterSettings reg)
        {
            return lastReadInfos.FirstOrDefault(i => i.DeviceName == dev.DeviceName && i.RegisterNumber == reg.Number && i.RegType == reg.RegType);
        }

        private void SetLastReadTime(DeviceSettings dev, RegisterSettings reg, DateTime lastReadTime)
        {
            var info = GetLastReadInfo(dev, reg);
            if (info == null)
            {
                info = new LastReadInfo()
                {
                    DeviceName = dev.DeviceName,
                    RegisterNumber = reg.Number,
                    RegType = reg.RegType,
                    LastReadTime = lastReadTime
                };
                lastReadInfos.Add(info);
            }
            else
            {
                info.LastReadTime = lastReadTime;
            }
        }

        private DateTime GetNextReadTime(DeviceSettings dev, RegisterSettings reg)
        {
            return GetLastReadInfo(dev, reg)?.LastReadTime ?? DateTime.MinValue
                + reg.ReadPeriod;
        }

        private (DateTime? waitForTime, ReadTaskRequest? readTaskRequest) GetReadTask()
        {
            var currTime = DateTime.Now;

            var pending = settings.Devices.SelectMany(d => d.Registers.Select(r => new { Device = d, Register = r, NextReadTime = GetNextReadTime(d, r) }))
                .OrderBy(r => r.NextReadTime)
                .FirstAndFilterdByFirst((f, c) =>
                {
                    if (c.Device.DeviceName != f.Device.DeviceName) return false;
                    if (c.Register.RegType != f.Register.RegType) return false;
                    if (c.NextReadTime > currTime) return false;
                    if (f.Register.RegType.IsBitReg())
                    {
                        if (Math.Abs(c.Register.Number - f.Register.Number) + 1 > settings.MaxReadBit) return false;
                    }
                    else
                    {
                        if (Math.Abs(c.Register.Number - f.Register.Number) + 1 > settings.MaxReadRegisters) return false;
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
                    var holeSize = p.Register.Number - c.Register.Number - 1;
                    if (p.Register.RegType.IsBitReg())
                    {
                        return holeSize <= settings.MaxBitHole;
                    }
                    else
                    {
                        return holeSize <= settings.MaxRegHole;
                    }
                })
                .ToArray();

            var after = pending
                .Where(r => r.Register.Number >= first.Register.Number)
                .OrderBy(r => r.Register.Number)
                .TakeWhileWithPrev((p, c) => {
                    var holeSize = c.Register.Number - p.Register.Number - 1;
                    if (p.Register.RegType.IsBitReg())
                    {
                        return holeSize <= settings.MaxBitHole;
                    }
                    else
                    {
                        return holeSize <= settings.MaxRegHole;
                    }
                })
                .ToArray();

            var startNumber = first.Register.Number;
            var endNumber = first.Register.Number;
            var indBefore = 1;
            var indAfter = 1;
            var maxRead = first.Register.RegType.IsBitReg() ? settings.MaxReadBit : settings.MaxReadRegisters;

            while (true)
            {
                var afterItem = indAfter < after.Length ? after[indAfter] : null;
                var beforeItem = indBefore < before.Length ? before[indBefore] : null;
                bool allowAfter = afterItem != null && (afterItem.Register.Number - startNumber + 1 <= maxRead);
                bool allowBefore = beforeItem != null && (endNumber - beforeItem.Register.Number + 1 <= maxRead);

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
                    endNumber = afterItem!.Register.Number;
                }
                else
                {
                    indBefore++;
                    startNumber = beforeItem!.Register.Number;
                }
            }

            return (null, new ReadTaskRequest(first.Device, first.Register.RegType, startNumber, endNumber - startNumber + 1));
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            using var port = new SerialPort(settings.PortName);

            port.BaudRate = settings.BaudRate;
            port.DataBits = settings.DataBits;
            port.Parity = settings.Parity;
            port.StopBits = settings.StopBits;
            port.Open();

            using var master = modbusFactory.CreateRtuMaster(port);

            using var regHandle = queueRepository.RegisterQueue(this, settings.PortName);

            while (!cancellationToken.IsCancellationRequested)
            {
                (var waitForTime, var readTaskRequest) = GetReadTask();
                
                if (waitForTime.HasValue)
                {
                    var delay = waitForTime.Value - DateTime.Now;
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                await PerfomReadRequest(master, readTaskRequest!);
            }
        }

        private async Task PerfomReadRequest(IModbusMaster modbus, ReadTaskRequest readTask)
        {
            var slaveAddress = byte.Parse(readTask.Device.DeviceName);
            var startReg = (ushort)readTask.StartRegister;
            var regCount = (ushort)readTask.RegisterCount;
            var readTime = DateTime.Now;

            IEnumerable<object> readResult;

            switch (readTask.RegType)
            {
                case RegisterType.Coil:
                    readResult = (await modbus.ReadCoilsAsync(slaveAddress, startReg, regCount)).Cast<object>();
                    break;
                case RegisterType.DiscreteInput:
                    readResult = (await modbus.ReadInputsAsync(slaveAddress, startReg, regCount)).Cast<object>();
                    break;
                case RegisterType.HoldingRegister:
                    readResult = (await modbus.ReadHoldingRegistersAsync(slaveAddress, startReg, regCount)).Cast<object>();
                    break;
                case RegisterType.InputRegister:
                    readResult = (await modbus.ReadInputRegistersAsync(slaveAddress, startReg, regCount)).Cast<object>();
                    break;
                default:
                    throw new NotImplementedException();
            }

            var publishItems = new List<PublishItem>();
            int currNumber = readTask.StartRegister;
            foreach(var regValue in readResult)
            {
                var registers = readTask.Device.Registers.Where(r => r.Number == currNumber && r.RegType == readTask.RegType).ToArray();
                publishItems.AddRange(registers.Select(r => new PublishItem(r.Name, regValue)));
                foreach(var reg in registers)
                {
                    SetLastReadTime(readTask.Device, reg, readTime);
                }
                currNumber++;
            }
        }

    }
}
