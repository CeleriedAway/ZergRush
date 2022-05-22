using System.Collections.Generic;

namespace ZergRush.ReactiveCore
{
    /// Reactive collection abstraction.
    /// Collection of data that is changed over time
    public interface IReactiveCollection<out T> : IReadOnlyList<T>
    {
        IEventStream<IReactiveCollectionEvent<T>> update { get; }
    }
    
    public enum ReactiveCollectionEventType : byte
    {
        Reset,
        Insert,
        Remove,
        Set,
    }

    public interface IReactiveCollectionEvent<out T>
    {
        ReactiveCollectionEventType type { get; }
        int position { get; }
        
        T newItem { get; }
        T oldItem { get; }
        IReadOnlyList<T> oldData { get; }
        IReadOnlyList<T> newData { get; }
    }

    public sealed class ReactiveCollectionEvent<T> : IReactiveCollectionEvent<T>
    {
        public ReactiveCollectionEventType type { get; set; }
        public int position { get; set; }
        public T newItem { get; set; }
        public T oldItem { get; set; }
        public IReadOnlyList<T> oldData { get; set; }
        public IReadOnlyList<T> newData { get; set; }
    }
}