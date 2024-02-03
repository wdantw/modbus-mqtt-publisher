using ModbusMqttPublisher.Server.Contracts;

namespace ModbusMqttPublisher.Server.Services.Queues
{
	public interface IReadQueueRegister
	{
		int StartNumber { get; }
		int EndNumber { get; }
		DateTime NextReadTime { get; }
		RegisterType RegisterType { get; }
		event Action NextReadTimeChanged;
	}

	public interface IReadQueueDevice<TRegister>
		where TRegister : IReadQueueRegister
	{
		TRegister[] Registers { get; }
		DateTime DeviceNextReadTime { get; }
		int MaxRegHole { get; }
		int MaxBitHole { get; }
		int MaxReadRegisters { get; }
		int MaxReadBit { get; }
	}

}
