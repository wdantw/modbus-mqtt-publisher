namespace ModbusMqttPublisher.Server.Domain
{
    public abstract class ReadComparableGroup<TThisType, TChildType> : ReadComparableGroupBase<TChildType>
        where TChildType : class, IReadPriorityComparable
        where TThisType : ReadComparableGroup<TThisType, TChildType>
    {
        private readonly IReadPriorityCallbacks<TThisType> _callbacks;
        protected abstract TThisType This { get; }

        public ReadComparableGroup(IReadPriorityCallbacks<TThisType> callbacks)
        {
            _callbacks = callbacks;
        }


        protected override void ThisPriorityUp(DateTime accessTime)
            => _callbacks.ChildItemPriorityUp(This, accessTime);

        protected override void ThisPriorityDown(DateTime accessTime)
            => _callbacks.ChildItemPriorityDown(This, accessTime);

        protected override void ChildItemAccessFailed(TChildType changedItem, DateTime accessTime)
            => _callbacks.ChildItemAccessFailed(This, accessTime);

    }
}
