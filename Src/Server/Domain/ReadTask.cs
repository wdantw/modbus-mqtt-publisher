namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadTask
    {
        public ReadTask(ArraySegment<ReadRegister> registers)
        {
            Registers = registers;
        }

        public ushort StartNumber => Registers[0].StartNumber;
        public ushort EndNumber => Registers[^1].EndNumber;

        public ArraySegment<ReadRegister> Registers { get; }
    }
}
