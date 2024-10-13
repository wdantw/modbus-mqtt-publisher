using Xunit;

namespace ModbusMqttPublisher.Tests.Hosts
{
    [CollectionDefinition(nameof(MqttTestHostCollection))]
    public class MqttTestHostCollection : ICollectionFixture<MqttTestHost>
    {
    }
}
