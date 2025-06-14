
namespace ModbusMqttPublisher.Server.Domain
{
    public abstract class ReadComparableGroupBase<TChildType> : IReadPriorityComparable, IReadPriorityCallbacks<TChildType>
        where TChildType : class, IReadPriorityComparable
    {
        private TChildType? _mostPriorityItem = null;

        protected abstract TChildType[] Items { get; }
        public virtual DateTime NextReadTime => EnsureMostPrioriyItem().NextReadTime;

        protected virtual void ThisPriorityUp(DateTime accessTime) { }
        protected virtual void ThisPriorityDown(DateTime accessTime) { }

        protected virtual void ChildItemPriorityUp(TChildType changedItem, DateTime accessTime)
        {
            // приоритет итема повысится и он может стать самым приоритетным
            if (_mostPriorityItem != null && _mostPriorityItem != changedItem)
            {
                if (changedItem.HasMorePriorityForRead(_mostPriorityItem))
                {
                    _mostPriorityItem = changedItem;
                    ThisPriorityUp(accessTime);
                }
            }
            else
            {
                // Приоритет всей группы при повышении может только повысится
                ThisPriorityUp(accessTime);
            }
        }

        protected virtual void ChildItemPriorityDown(TChildType changedItem, DateTime accessTime)
        {
            // приоритет итема понизился. если он был приоритетным, то возможно приоритет получит другой
            if (_mostPriorityItem != null)
            {
                if (changedItem == _mostPriorityItem)
                {
                    // прочитанный регистр был самым приоритетным в группе, значит возможно приоритет получит другой
                    _mostPriorityItem = null;
                    ThisPriorityDown(accessTime);
                }
            }
            else
            {
                // приоритет всей группы может только понизится
                ThisPriorityDown(accessTime);
            }
        }

        protected virtual void ChildItemAccessFailed(TChildType changedItem, DateTime accessTime) { }

        void IReadPriorityCallbacks<TChildType>.ChildItemPriorityUp(TChildType changedItem, DateTime accessTime)
            => ChildItemPriorityUp(changedItem, accessTime);

        void IReadPriorityCallbacks<TChildType>.ChildItemPriorityDown(TChildType changedItem, DateTime accessTime)
            => ChildItemPriorityDown(changedItem, accessTime);

        void IReadPriorityCallbacks<TChildType>.ChildItemAccessFailed(TChildType changedItem, DateTime accessTime)
            => ChildItemAccessFailed(changedItem, accessTime);

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
    }
}
