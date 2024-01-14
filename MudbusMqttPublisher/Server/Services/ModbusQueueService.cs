using MudbusMqttPublisher.Server.Contracts.Settings;
using NModbus;
using System.IO.Ports;
using NModbus.Serial;
using MudbusMqttPublisher.Server.Common;
using MudbusMqttPublisher.Server.Contracts;
using System.Text;

namespace MudbusMqttPublisher.Server.Services
{
    public class ModbusQueueService : IQueueService
    {
        private class LastReadInfo
        {
            public string Name { get; }
            public DateTime LastReadTime { get; set; }
            public object LastReadValue { get; set; }

            public LastReadInfo(string name, DateTime lastReadTime, object lastReadValue)
            {
                Name = name;
                LastReadTime = lastReadTime;
                LastReadValue = lastReadValue;
            }
        }

        private readonly PortSettings settings;
        private readonly ILogger<ModbusQueueService> logger;
        private readonly IQueueRepository queueRepository;
        private readonly IModbusFactory modbusFactory;

        private List<LastReadInfo> lastReadInfos = new List<LastReadInfo>();


        public ModbusQueueService(PortSettings settings, ILogger<ModbusQueueService> logger, IQueueRepository queueRepository, IModbusFactory modbusFactory)
        {
            this.settings = settings;
            this.logger = logger;
            this.queueRepository = queueRepository;
            this.modbusFactory = modbusFactory;
        }

        private LastReadInfo? GetLastReadInfo(DeviceSettings dev, RegisterSettings reg)
        {
            return lastReadInfos.FirstOrDefault(i => i.Name == reg.Name);
        }

        private bool SetLastReadTime(DeviceSettings dev, RegisterSettings reg, DateTime lastReadTime, object value)
        {
            var info = GetLastReadInfo(dev, reg);
            if (info == null)
            {
                info = new LastReadInfo(
                    reg.Name,
                    lastReadTime,
                    value
                    );
                lastReadInfos.Add(info);
                return true;
            }
            else
            {
                var changed = object.Equals(info.LastReadValue, value);
                info.LastReadTime = lastReadTime;
                info.LastReadValue = value;
                return changed;
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
                await Task.Delay(settings.MinSleepTimeout, cancellationToken);
            }
        }

        private async Task PerfomReadRequest(IModbusMaster modbus, ReadTaskRequest readTask)
        {
            var slaveAddress = readTask.Device.SlaveAddress;
            var startReg = (ushort)readTask.StartRegister;
            var regCount = (ushort)readTask.RegisterCount;
            var readTime = DateTime.Now;

            object[] readResult;

            switch (readTask.RegType)
            {
                case RegisterType.Coil:
                    readResult = (await modbus.ReadCoilsAsync(slaveAddress, startReg, regCount)).Cast<object>().ToArray();
                    break;
                case RegisterType.DiscreteInput:
                    readResult = (await modbus.ReadInputsAsync(slaveAddress, startReg, regCount)).Cast<object>().ToArray();
                    break;
                case RegisterType.HoldingRegister:
                    readResult = (await modbus.ReadHoldingRegistersAsync(slaveAddress, startReg, regCount)).Cast<object>().ToArray();
                    break;
                case RegisterType.InputRegister:
                    readResult = (await modbus.ReadInputRegistersAsync(slaveAddress, startReg, regCount)).Cast<object>().ToArray();
                    break;
                default:
                    throw new NotImplementedException();
            }

            var publishItems = new List<PublishItem>();
            int endRegister = readTask.StartRegister + readTask.RegisterCount;
            for(int i = 0; i < readTask.RegisterCount; i++)
            {
                int currNumber = readTask.StartRegister + i;
                var registers = readTask.Device.Registers.Where(r => r.Number == currNumber && r.RegType == readTask.RegType && r.EndRegisterNumber <= endRegister);
                foreach(var reg in registers)
                {
                    object regValue = readResult[i];
                    if (!reg.RegType.IsBitReg())
                    {
                        switch (reg.RegFormat)
                        {
                            case RegisterFormat.Uint16:
                                regValue = (ushort)regValue;
                                break;
                            case RegisterFormat.Uint32:
                                regValue = ToUint32((ushort)regValue, (ushort)readResult[i + 1]);
                                break;
                            case RegisterFormat.Uint64:
                                regValue = ToUint64((ushort)regValue, (ushort)readResult[i + 1], (ushort)readResult[i + 2], (ushort)readResult[i + 3]);
                                break;
                            case RegisterFormat.Int16:
                                {
                                    var t = (ushort)regValue;
                                    regValue = unchecked((short)t);
                                }
                                break;
                            case RegisterFormat.Int32:
                                {
                                    var t = ToUint32((ushort)regValue, (ushort)readResult[i + 1]);
                                    regValue = unchecked((int)t);
                                }
                                break;
                            case RegisterFormat.Int64:
                                {
                                    var t = ToUint64((ushort)regValue, (ushort)readResult[i + 1], (ushort)readResult[i + 2], (ushort)readResult[i + 3]);
                                    regValue = unchecked((long)t);
                                }
                                break;
                            case RegisterFormat.String:
                                regValue = Encoding.ASCII.GetString(
                                    readResult.Skip(i).Take(reg.Length!.Value).Cast<ushort>().SelectMany(v => new byte[] { (byte)((v & 0xFF00) >> 8), (byte)(v & 0xFF) })
                                    .ToArray()
                                );
                                break;
                        }

                    }

                    if (SetLastReadTime(readTask.Device, reg, readTime, regValue))
                        publishItems.Add(new PublishItem(reg.Name, regValue));
                }
                currNumber++;
            }
        }

        static uint ToUint32(ushort v1, ushort v2) => ((uint)v1 << 16) + v2;

        static ulong ToUint64(ushort v1, ushort v2, ushort v3, ushort v4) => ((ulong)ToUint32(v1, v2) << 32) + ToUint32(v3, v4);

    }
}
