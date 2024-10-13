using ModbusMqttPublisher.Server.Contracts.Configs;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadDevice : ReadComparableGroup<ReadDevice, ReadRegisterGroup>
    {
        private readonly ReadRegisterGroup[] _groups;

        protected override ReadRegisterGroup[] Items => _groups;
        protected override ReadDevice This => this;

        public ReadDevice(
            ModbusDeviceComplete devSettings,
            PriorityChangedDelegate<ReadDevice> priorityUpCallback,
            PriorityChangedDelegate<ReadDevice> priorityDownCallback)
            : base (
                  priorityUpCallback: priorityUpCallback,
                  priorityDownCallback: priorityDownCallback)
        {
            _groups = devSettings.Registers
                .GroupBy(s => s.RegType)
                .Select(g => new ReadRegisterGroup(
                    regSettings: g.OrderBy(r => r.Number).ThenBy(r => r.Length ?? 1),
                    priorityUpCallback: i => ChildItemPriorityUp(i),
                    priorityDownCallback: i => ChildItemPriorityDown(i)
                    )
                ).ToArray();
        }
    }
}
