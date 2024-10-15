using ModbusMqttPublisher.Server.Contracts.Configs;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadPort : ReadComparableGroup<ReadPort, ReadDevice>
    {
        private readonly ReadDevice[] _devices;

        protected override ReadDevice[] Items => _devices;
        protected override ReadPort This => this;
        public ReadDevice[] Devices => _devices;

        public ReadPort(
            ModbusPortComplete portSettings,
            PriorityChangedDelegate<ReadPort> priorityUpCallback,
            PriorityChangedDelegate<ReadPort> priorityDownCallback)
            : base(
                  priorityUpCallback: priorityUpCallback,
                  priorityDownCallback: priorityDownCallback)
        {
            _devices = portSettings.Devices
                .Select(d => new ReadDevice(
                    devSettings: d,
                    priorityUpCallback: i => ChildItemPriorityUp(i),
                    priorityDownCallback: i => ChildItemPriorityDown(i)
                    )
                ).ToArray();
        }
    }
}
