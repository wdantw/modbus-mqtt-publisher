namespace ModbusMqttPublisher.Server.Domain
{
    public abstract class ReadComparableGroup<TThisType, TChildType> : IReadPriorityComparable<TThisType>
        where TChildType : class, IReadPriorityComparable<TChildType>
        where TThisType : ReadComparableGroup<TThisType, TChildType>
    {
        private readonly PriorityChangedDelegate<TThisType> _priorityUpCallback;
        private readonly PriorityChangedDelegate<TThisType> _priorityDownCallback;
        private TChildType? _mostPriorityItem = null;

        protected abstract TChildType[] Items { get; }
        protected abstract TThisType This { get; }

        public ReadComparableGroup(
            PriorityChangedDelegate<TThisType> priorityUpCallback,
            PriorityChangedDelegate<TThisType> priorityDownCallback)
        {
            _priorityUpCallback = priorityUpCallback;
            _priorityDownCallback = priorityDownCallback;
        }

        protected void ChildItemPriorityUp(TChildType changedItem)
        {
            // приоритет итема повысится и он может стать самым приоритетным
            if (_mostPriorityItem != null && _mostPriorityItem != changedItem)
            {
                if (changedItem.HasMorePriorityForRead(_mostPriorityItem))
                {
                    _mostPriorityItem = changedItem;
                    _priorityUpCallback(This);
                }
            }
            else
            {
                _priorityUpCallback(This);
            }
        }

        protected void ChildItemPriorityDown(TChildType register)
        {
            // приоритет итема понизился. если он был приоритетным, то возможно приоритет получит другой
            if (_mostPriorityItem != null)
            {
                if (register == _mostPriorityItem)
                {
                    // прочитанный регистр был самым приоритетным в группе, значит возможно приоритет получит другой
                    _mostPriorityItem = null;
                    _priorityDownCallback(This);
                }
            }
            else
            {
                _priorityDownCallback(This);
            }
        }

        public TChildType EnsureMostPrioriyItem()
        {
            if (_mostPriorityItem == null)
            {
                _mostPriorityItem = Items[0];
                foreach (var item in Items.Skip(1))
                {
                    if (item.HasMorePriorityForRead(_mostPriorityItem))
                        _mostPriorityItem = item;
                }
            }
            return _mostPriorityItem;
        }

        public bool HasMorePriorityForRead(TThisType other)
        {
            return EnsureMostPrioriyItem().HasMorePriorityForRead(other.EnsureMostPrioriyItem());
        }
    }
}
