using ModbusMqttPublisher.Server.Contracts.Configs;

namespace ModbusMqttPublisher.Server.Domain
{
    public class ReadPort : ReadComparableGroupBase<ReadDevice>
    {
        private readonly ReadDevice[] _devices;

        protected override ReadDevice[] Items => _devices;
        public ReadDevice[] Devices => _devices;

        public ReadPort(ModbusPortComplete portSettings)
        {
            _devices = portSettings.Devices
                .Select(d => new ReadDevice(d, this)
                ).ToArray();
        }

        public ReadTask? GetNextReadTask(
            int maxRegisterCount,
            int maxHoleSize,
            DateTime currTime)
        {
            var nextGroup = EnsureMostPrioriyItem().EnsureMostPrioriyItem();
            return nextGroup.GetReadTask(maxRegisterCount, maxHoleSize, currTime);
        }
    }
}
