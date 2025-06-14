namespace ModbusMqttPublisher.Server.Domain
{
    public delegate void PriorityChangedDelegate<in T>(T item, DateTime accessTime);
}
