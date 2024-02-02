using MudbusMqttPublisher.Server.Common;
using MudbusMqttPublisher.Server.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace MudbusMqttPublisher.Server.Services.Queues
{
	public class ReadQueue<TDevice, TRegister>
		where TDevice : class, IReadQueueDevice<TRegister>
		where TRegister : IReadQueueRegister
	{
		private class GroupInfo
		{
			public RegisterType RegisterType { get; }
			public int HotestRegister { get; private set; }
			public TRegister[] Registers { get; }
			public DateTime NextReadTimme => Registers[HotestRegister].NextReadTime;

			public GroupInfo(TRegister[] sortedRegisterCollection, RegisterType registerType)
			{
				Registers = sortedRegisterCollection;
				for (int i = 0; i < Registers.Length; i++)
				{
					var regIndex = i;
					Registers[regIndex].NextReadTimeChanged += () => NextReadTimeChanged(regIndex);
				}
				UpdateHottestRegister();
				RegisterType = registerType;
			}

			private void UpdateHottestRegister()
			{
				HotestRegister = 0;
				for (int i = 1; i < Registers.Length; i++)
				{
					if (Registers[i].NextReadTime < Registers[HotestRegister].NextReadTime)
						HotestRegister = i;
				}
			}

			private void NextReadTimeChanged(int regIndex)
			{
				if (regIndex == HotestRegister)
				{
					UpdateHottestRegister();
				}
				else
				{
					var currReg = Registers[regIndex];
					if (currReg.NextReadTime < Registers[HotestRegister].NextReadTime)
						HotestRegister = regIndex;
				}
			}
		}

		private class DeviceInfo
		{
			public TDevice Device { get; }
			public DateTime DeviceNextReadTime => Device.DeviceNextReadTime;
			public GroupInfo[] Groups { get; }

			public DeviceInfo(GroupInfo[] groups, TDevice device)
			{
				Groups = groups;
				Device = device;
			}
		}

		private DateTime portNextReadTime = DateTime.MinValue;
		private List<DeviceInfo> devices = new ();

		public ReadQueue(IEnumerable<TDevice> portDevices)
		{
			foreach (var devSettings in portDevices)
			{
				var groups = new List<GroupInfo>();

				foreach (var regType in RegisterTypeMetadata.Types)
				{
					var registers = devSettings.Registers
						.Where(r => r.RegisterType == regType)
						.OrderBy(r => r.StartNumber).ThenBy(r => r.EndNumber)
						.ToArray();

					if (registers.Length > 0)
						groups.Add(new GroupInfo(registers, regType));
				}

				devices.Add(new DeviceInfo(groups.ToArray(), devSettings));
			}
		}

		public void SetPortNextReadTime(DateTime portNextReadTime)
		{
			this.portNextReadTime = portNextReadTime;
		}

		public bool GetReadTask(
			DateTime currTime,
			[NotNullWhen(true)] out ArraySegment<TRegister>? readTask,
			[NotNullWhen(true)] out TDevice? readDevice,
			[NotNullWhen(true)] out RegisterType? registerType,
			out DateTime nextReadTime
			)
		{
			readTask = null;
			if (FindNextReadGroup(currTime, out var nextReadGroup, out readDevice, out registerType, out nextReadTime))
			{
				var maxTaskSize = registerType.Value.IsBitReg() ? readDevice.MaxReadBit : readDevice.MaxReadRegisters;
				var maxHoleSize = registerType.Value.IsBitReg() ? readDevice.MaxBitHole : readDevice.MaxRegHole;
				var granulation = registerType.Value.IsBitReg() ? 8 : 1;

				readTask = GetTask(nextReadGroup.Registers, nextReadGroup.HotestRegister, maxTaskSize, maxHoleSize, currTime, granulation);
				return true;
			}
			else
			{
				return false;
			}
		}

		private bool FindNextReadGroup(
			DateTime currTime,
			[NotNullWhen(true)] out GroupInfo? hotestGroup,
			[NotNullWhen(true)] out TDevice? hotestDevice,
			[NotNullWhen(true)] out RegisterType? hotestRegisterType,
			out DateTime nextReadTime)
		{
			hotestGroup = null;
			hotestDevice = null;
			hotestRegisterType = null;
			nextReadTime = DateTime.MaxValue;

			if (portNextReadTime > currTime)
			{
				nextReadTime = portNextReadTime;
				return false;
			}

			foreach(var dev in devices)
			{
				if (dev.DeviceNextReadTime > nextReadTime)
					continue;

				if (dev.DeviceNextReadTime > currTime)
				{
					// устройство не готово к чтению, но время его следущего чтения ближе чем у остальных просмотренных, которые тоже не готовы
					nextReadTime = dev.DeviceNextReadTime;
					continue;
				}

				// устройство готово
				foreach (var group in dev.Groups)
				{
					var groupNextReadTime = group.NextReadTimme;
					
					if (groupNextReadTime > nextReadTime)
						continue;

					nextReadTime = groupNextReadTime;

					if (groupNextReadTime <= currTime)
					{
						hotestGroup = group;
						hotestDevice = dev.Device;
						hotestRegisterType = group.RegisterType;
					}
				}
			}

			return hotestGroup != null;
		}

		private ArraySegment<TRegister> GetTask(
			TRegister[] sortedRegistersList,
			int hotestIndex,
			int maxTaskSize,
			int maxHoleSize,
			DateTime currTime,
			int granulation
			)
		{
			// TODO: учесть гранулярность 
			// идея примерно такая - первый startIndex выбрать на granulation-1 меньше

			var startIndex = Math.Max(0, hotestIndex - maxTaskSize);
			while (sortedRegistersList[startIndex].NextReadTime > currTime)
			{
				startIndex++;
			}

			var endIndex = startIndex + 1;
			var hotInRange = startIndex == hotestIndex;
			for (int i = startIndex + 1; i < sortedRegistersList.Length; i++)
			{
				var holeSize = sortedRegistersList[i].StartNumber - sortedRegistersList[endIndex - 1].EndNumber;
				bool holeSizeExceeded = holeSize > maxHoleSize;

				if (holeSizeExceeded && hotInRange)
				{
					break;
				}

				if (sortedRegistersList[i].NextReadTime > currTime)
				{
					// его время не настало, в расчете не учитываем
					continue;
				}

				var newStart = startIndex;

				if (holeSizeExceeded)
				{
					// hotInRange == false, иначе вышли бы из цикла раньше
					newStart = i;
				}
				else
				{
					bool cancelMove = false;

					while (sortedRegistersList[i].EndNumber - sortedRegistersList[newStart].StartNumber > maxTaskSize)
					{
						cancelMove = hotInRange && sortedRegistersList[newStart].NextReadTime < sortedRegistersList[i].NextReadTime;
						
						if (cancelMove)
							break;

						newStart++;
					}

					if (cancelMove)
					{
						// если у следующего регистра может оказаться меньший EndNumber (например он поддиапазон) ,то тут брейк ставить не надо
						// а надо выполнить следующий цикл
						break;
					}

					if (newStart != startIndex)
					{
						while (sortedRegistersList[newStart].NextReadTime > currTime)
						{
							newStart++;
							if (newStart == endIndex)
							{
								newStart = i;
								break;
							}
						}
					}
				}

				startIndex = newStart;
				endIndex = i + 1;
				hotInRange = hotInRange || startIndex == hotestIndex;
			}

			return sortedRegistersList.GetSegment(startIndex, endIndex - startIndex);
		}
	}
}
