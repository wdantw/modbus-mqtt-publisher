namespace ModbusMqttPublisher.Server.Domain
{
    public interface IReadPriorityCallbacks<TChildType>
    {
        void ChildItemPriorityUp(TChildType changedItem, DateTime accessTime);
        void ChildItemPriorityDown(TChildType changedItem, DateTime accessTime);
        void ChildItemAccessFailed(TChildType changedItem, DateTime accessTime);
    }
}
