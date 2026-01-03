namespace ModbusMqttPublisher.Server.Domain.FindRegRange
{
    public interface IRegisterCollectionState
    {
        int Count { get; }

        int GetMostPriorityItemIndex();
        
        bool NeedReadingNow(int index);

        ushort StartAddress(int index);
        
        ushort EndAddress(int index);

        bool HasMorePriority(int index1, int index2);
    }
}
