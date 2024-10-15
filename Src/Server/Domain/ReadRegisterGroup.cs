using ModbusMqttPublisher.Server.Contracts.Configs;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadRegisterGroup : ReadComparableGroup<ReadRegisterGroup, ReadRegister>
    {
        private readonly ReadRegister[] _registers;
        protected override ReadRegister[] Items => _registers;
        protected override ReadRegisterGroup This => this;
        public ReadRegister[] Registers => _registers;

        public ReadRegisterGroup(
            IEnumerable<ModbusRegisterCompleted> regSettings,
            PriorityChangedDelegate<ReadRegisterGroup> priorityUpCallback,
            PriorityChangedDelegate<ReadRegisterGroup> priorityDownCallback)
            : base(
                  priorityUpCallback: priorityUpCallback,
                  priorityDownCallback: priorityDownCallback)
        {
            _registers = regSettings
                .Select(set =>
                    new ReadRegister(
                        settings: set,
                        priorityUpCallback: i => ChildItemPriorityUp(i),
                        priorityDownCallback: i => ChildItemPriorityDown(i)
                        )
                )
                .ToArray();
        }
    }
}
