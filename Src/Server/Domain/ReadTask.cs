using ModbusMqttPublisher.Server.Contracts;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadTask
    {
        public ReadTask(ArraySegment<ReadRegister> registers, ReadDevice device)
        {
            Registers = registers;
            Device = device;
        }


        public ushort StartNumber => Registers[0].StartNumber;
        public ushort EndNumber => Registers[^1].EndNumber;
        public ushort RegisterCount => (ushort)(EndNumber - StartNumber);
        public RegisterType RegisterType => Registers[0].RegisterType;

        public ReadDevice Device { get; }
        public ArraySegment<ReadRegister> Registers { get; }

        public void AccessFailed(DateTime accessTime)
        {
            foreach (var r in Registers)
                r.AccessFailed(accessTime);
        }
    }
}
