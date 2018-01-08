#if UNITY_5_3_OR_NEWER

using System;
using ZergRush.ReactiveCore;

namespace ZergRush.ReactiveUI
{
    public interface ITableContentProvider<TView>
    {
        void FillView(TView view, int index);
        int count { get; }
        IEventStream<ReactiveCollectionEvent> updates { get; }
    }

    public class TableContentProvider<TData, TView> : ITableContentProvider<TView>
    {
        IReactiveCollection<TData> data;
        Action<TData, TView> factory;
        public TableContentProvider(IReactiveCollection<TData> data, Action<TData, TView> factory)
        {
            this.data = data;
            this.factory = factory;
        }

        public void FillView(TView view, int index)
        {
            factory(data.current[index], view);
        }

        public int count { get { return data.current.Count; } }

        public IEventStream<ReactiveCollectionEvent> updates
        {
            get { return data.update.Map(e => (ReactiveCollectionEvent) e); }
        }
    }
}
#endif
