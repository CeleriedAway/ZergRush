using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#else
class SerializeField : Attribute {}
#endif

namespace ZergRush.ReactiveCore
{
    public enum ReactiveCollectionEventType : byte
    {
        Reset,
        Insert,
        Remove,
        Set
    }

    public class ReactiveCollectionEvent
    {
        public ReactiveCollectionEventType type;
        public int position;
    }

    public class ReactiveCollectionEvent<T> : ReactiveCollectionEvent
    {
        public T newItem;
        public T oldItem;
        public IEnumerable<T> oldData;
        public IEnumerable<T> newData;
    }
    
    /*
          Reactive collection abstraction.
          Main usecase is presentation of some collections of data in tables.
     */

    public interface IReactiveCollection<T> : IEnumerable<T>
    {
        IEventStream<ReactiveCollectionEvent<T>> update { get; }
        List<T> current { get; }
    }

    [DebuggerDisplay("{this.ToString()}")]
    public class ReactiveCollection<T> : IReactiveCollection<T>
    {
        protected EventStream<ReactiveCollectionEvent<T>> up;
        protected List<T> data;

        public List<T> current
        {
            get { return data; }
            set { Reset(value); }
        }

        public ReactiveCollection()
        {
            this.data = new List<T>();
        }
        
        public ReactiveCollection(IEnumerable<T> list)
        {
            this.data = list.ToList();
        }

        public ReactiveCollection(List<T> list)
        {
            this.data = list;
        }

        public IEventStream<ReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        public void Add(T item)
        {
            data.Add(item);
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Insert,
                    newItem = item,
                    position = data.Count - 1,
                });
        }

        public void Insert(int index, T item)
        {
            data.Insert(index, item);
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Insert,
                    newItem = item,
                    position = index
                });
        }

        public void Remove(T item)
        {
            RemoveAt(data.IndexOf(item));
        }

        public void RemoveAt(int index)
        {
            var item = data[index];
            data.RemoveAt(index);
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Remove,
                    position = index,
                    oldItem = item
                });
        }

        public void Reset(List<T> newData)
        {
            var oldData = data;
            data = newData;
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Reset,
                    oldData = oldData,
                    newData = data
                });
        }

        public void Reset(IEnumerable<T> val = null)
        {
            var oldData = data;
            data = val != null ? new List<T>(val) : new List<T>();
            if (oldData.Count == 0 && data.Count == 0) return;
            
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Reset,
                    oldData = oldData,
                    newData = val
                });
        }

        public T this[int index]
        {
            get { return data[index]; }
            set
            {
                var oldItem = data[index];
                data[index] = value;
                if (up != null)
                {
                    up.Send(new ReactiveCollectionEvent<T> {
                        type = ReactiveCollectionEventType.Set,
                        position = index,
                        newItem = value,
                        oldItem = oldItem
                    });
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return current.PrintCollection();
        }
        
        public int Count
        {
            get { return data.Count; }
        }
    }

    public abstract class AbstractCollectionTransform<T> : IReactiveCollection<T>
    {
        int connectionCounter = 0;
        IDisposable collectionConnection;
        
        protected readonly ReactiveCollection<T> buffer = new ReactiveCollection<T>();
        
        bool connected
        {
            get { return connectionCounter != 0; }
        }

        void OnConnect()
        {
            if (connectionCounter == 0)
            {
                RefillBuffer();
                collectionConnection = StartListen(); 
            }
            connectionCounter++;
        }

        protected abstract IDisposable StartListen();

        void OnDisconnect()
        {
            connectionCounter--;
            if (connectionCounter == 0)
            {
                collectionConnection.Dispose();
                collectionConnection = null;
                ClearBuffer();
            }
        }

        void ClearBuffer()
        {
            buffer.Reset();
        }

        protected void RefillBuffer()
        {
            Refill();
        }
        
        protected abstract void Refill();
        
        public IEnumerator<T> GetEnumerator()
        {
            return current.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEventStream<ReactiveCollectionEvent<T>> updateWrapp;

        public IEventStream<ReactiveCollectionEvent<T>> update
        {
            get
            {
                return updateWrapp ?? (updateWrapp = new AnonymousEventStream<ReactiveCollectionEvent<T>>(act =>
                {
                    OnConnect();
                    var connection = buffer.update.Listen(act);
                    return new AnonymousDisposable(() =>
                    {
                        OnDisconnect();
                        connection.Dispose();
                    });
                }));
            }
        }

        public List<T> current
        {
            get 
            { 
                if (!connected) RefillBuffer();
                return buffer.current;
            }
        }

        public override string ToString()
        {
            return current.PrintCollection();
        }
    }

    public class SingleElementCollection<T> : ICell<T>, IReactiveCollection<T>
    {
        [SerializeField] private T val;
        [NonSerialized] private EventStream<T> upVal;
        [NonSerialized] EventStream<ReactiveCollectionEvent<T>> up;

        public SingleElementCollection(T t)
        {
            val = t;
        }

        public SingleElementCollection()
        {
        }

        public T value
        {
            get { return val; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(value, val) == false)
                {
                    var oldval = val;
                    val = value;
                    if (upVal != null) upVal.Send(val);
                    if (up != null) up.Send(new ReactiveCollectionEvent<T>
                    {
                        type = ReactiveCollectionEventType.Set,
                        oldItem = oldval,
                        newItem = value
                    });
                }
            }
        }

        public EventStream<T> updates
        {
            get { return upVal = upVal ?? new EventStream<T>(); }
        }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Listen(callback);
        }

        public IDisposable OnChanged(Action action)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Listen(_ => action());
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public void SetValue(T v)
        {
            this.value = v;
        }
        

        public IEnumerator<T> GetEnumerator()
        {
            yield return value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return value;
        }

        public IEventStream<ReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        public List<T> current { get {return new List<T>{value};} }
    }
    
    public class SingleNullableElementCollection<T> : ICell<T>, IReactiveCollection<T> where T : class
    {
        [SerializeField] private T val;
        [NonSerialized] private EventStream<T> upVal;
        [NonSerialized] EventStream<ReactiveCollectionEvent<T>> up;

        public SingleNullableElementCollection(T t)
        {
            val = t;
        }

        public SingleNullableElementCollection()
        {
        }

        public T value
        {
            get { return val; }
            set
            {
                if (object.ReferenceEquals(value, val) == false)
                {
                    var oldval = val;
                    val = value;
                    if (upVal != null) upVal.Send(val);
                    if (up != null) {
                        if (oldval == null)
                        {
                            up.Send(new ReactiveCollectionEvent<T> {
                                type = ReactiveCollectionEventType.Insert,
                                newItem = val
                            });
                        }
                        else if (val == null)
                        {
                            up.Send(new ReactiveCollectionEvent<T> {
                                type = ReactiveCollectionEventType.Remove,
                                oldItem = oldval,
                            });
                        }
                        else
                        {
                            up.Send(new ReactiveCollectionEvent<T> {
                                type = ReactiveCollectionEventType.Set,
                                oldItem = oldval,
                                newItem = val
                            });
                        }
                    }
                }
            }
        }

        public EventStream<T> updates
        {
            get { return upVal = upVal ?? new EventStream<T>(); }
        }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Listen(callback);
        }

        public IDisposable OnChanged(Action action)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Listen(_ => action());
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public void SetValue(T v)
        {
            this.value = v;
        }
        

        public IEnumerator<T> GetEnumerator()
        {
            yield return value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return value;
        }

        public IEventStream<ReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        public List<T> current { get {return new List<T>{value};} }
    }

    public static class ReactiveCollectionExtensions
    {
        class StaticCollection<T> : IReactiveCollection<T>
        {
            public List<T> list;

            public IEnumerator<T> GetEnumerator()
            {
                return list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEventStream<ReactiveCollectionEvent<T>> update
            {
                get { return AbandonedStream<ReactiveCollectionEvent<T>>.value; }
            }

            public List<T> current
            {
                get { return list; }
            }
        }

        public static IReactiveCollection<T> ToStaticReactiveCollection<T>(this List<T> coll)
        {
            return new StaticCollection<T> {list = coll};
        }

        public static ICell<int> CountCell<T>(this IReactiveCollection<T> coll)
        {
            return coll.AsCell().Map(c => c.Count);
        }

        public static IReactiveCollection<TMapped> Map<T, TMapped>(this IReactiveCollection<T> collection,
            Func<T, TMapped> mapFunc)
        {
            return new MappedCollection<T, TMapped>(collection, mapFunc);
        }

        public static IReactiveCollection<T> Filter<T>(this IReactiveCollection<T> collection,
            Func<T, bool> predicate)
        {
            return new FilteredCollection<T>(collection, predicate); 
        }
        
        // TODO Actually that is not difficult to implement this with simple filtered collection
        public static IReactiveCollection<T> Filter<T>(this IReactiveCollection<T> collection,
            Func<T, ICell<bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public static IDisposable BindEach<T>(this IReactiveCollection<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
            return collection.update.Listen(rce =>
            {
                switch (rce.type)
                {
                    case ReactiveCollectionEventType.Insert:
                    case ReactiveCollectionEventType.Set:
                        action(rce.newItem);
                        break;
                    case ReactiveCollectionEventType.Reset:
                        foreach (var item in collection)
                        {
                            action(item);
                        }
                        break;
                }
            });
        }
        
        public static IDisposable BindEach<T>(this IReactiveCollection<T> collection, Action<T> onInsert, Action<T> onRemove)
        {
            foreach (var item in collection)
            {
                onInsert(item);
            }
            return collection.update.Listen(rce =>
            {
                switch (rce.type)
                {
                    case ReactiveCollectionEventType.Insert:
                        onInsert(rce.newItem);
                        break;
                    case ReactiveCollectionEventType.Set:
                        onInsert(rce.newItem);
                        onRemove(rce.oldItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        onRemove(rce.oldItem);
                        break;
                    case ReactiveCollectionEventType.Reset:
                        foreach (var item in rce.oldData)
                        {
                            onRemove(item);
                        }
                        foreach (var item in rce.newData)
                        {
                            onInsert(item);
                        }
                        break;
                }
            });
        }
        
        public static IEventStream<T> MergeCollectionOfStreams<T>(this IReactiveCollection<IEventStream<T>> collection)
        {
            return new AnonymousEventStream<T>(action =>
            {
                var connections = new Connections();
                var disposable = new DoubleDisposable
                {
                    first = connections,
                    // TODO It can be done more effectively then asCell call but much more complex
                    second = collection.AsCell().Bind(coll =>
                    {
                        connections.DisconnectAll();
                        connections.AddRange(coll.Select(item => item.Listen(action)));
                    })
                };
                return disposable;
            });
        }

        public static ICell<T> AtIndex<T>(this IReactiveCollection<T> collection, int index)
        {
            return new AnonymousCell<T>(action =>
            {
                return collection.AsCell().ListenUpdates(coll =>
                {
                    action(coll.Count > index ? coll[index] : default(T));
                });
            }, () =>
            {
                var coll = collection.current;
                return coll.Count > index ? coll[index] : default(T);
            });
        }

        public static ICell<List<T>> AsCell<T>(this IReactiveCollection<T> collection)
        {
            return new AnonymousCell<List<T>>(action =>
            {
                return collection.update.Listen(_ => { action(collection.current); });
            }, () => collection.current);
        }

        public static IReactiveCollection<T> ToReactiveCollection<T>(this ICell<IEnumerable<T>> cell)
        {
            return new ReactiveCollectionFromCellOfArray<T>{cell = cell};    
        }
        
        [DebuggerDisplay("{this.ToString()}")]
        public class MappedCollection<T, TMapped> : AbstractCollectionTransform<TMapped>
        {
            readonly Func<T, TMapped> mapFunc;
            readonly IReactiveCollection<T> collection;
            
            public MappedCollection(IReactiveCollection<T> collection, Func<T, TMapped> mapFunc)
            {
                this.collection = collection;
                this.mapFunc = mapFunc;
            }

            void Process(ReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        RefillBuffer();
                        break;
                    case ReactiveCollectionEventType.Insert:
                        var item = mapFunc(e.newItem);
                        buffer.Insert(e.position, item);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        buffer.RemoveAt(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        var newItem = mapFunc(e.newItem);
                        buffer[e.position] = newItem;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListen()
            {
                return collection.update.Listen(Process);
            }

            protected override void Refill()
            {
                buffer.Reset(collection.current.Select(mapFunc));
            }
        }
        
        [DebuggerDisplay("{this.ToString()}")]
        public class FilteredCollection<T> : AbstractCollectionTransform<T>
        {
            readonly Func<T, bool> predicate;
            readonly IReactiveCollection<T> collection;
            List<int> realIndexes = new List<int>();
            
            public FilteredCollection(IReactiveCollection<T> collection, Func<T, bool> predicate)
            {
                this.collection = collection;
                this.predicate = predicate;
            }

            void Insert(int realIndex, T item)
            {
                int newIndex = 0;
                if (realIndexes.Count > 0)
                {
                    newIndex = realIndexes.UpperBound(realIndex);
                    for (var i = newIndex; i < realIndexes.Count; ++i)
                    {
                        realIndexes[i]++;
                    }
                }
                if (predicate(item) == false) return;
                realIndexes.Insert(newIndex, realIndex);
                buffer.Insert(newIndex, item);
            }

            void Remove(int realIndex)
            {
                if (realIndexes.Count == 0) return;
                var oldIndex = realIndexes.BinarySearch(realIndex);
                for (var i = oldIndex >= 0 ? oldIndex : ~oldIndex; i < realIndexes.Count; ++i)
                {
                    realIndexes[i]--;
                }
                if (oldIndex < 0) return;
                realIndexes.RemoveAt(oldIndex);
                buffer.RemoveAt(oldIndex);
            }

            void Process(ReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        RefillBuffer();
                        break;
                    case ReactiveCollectionEventType.Insert:
                        Insert(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        Remove(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        //TODO make proper set event resolve if needed
                        Remove(e.position); 
                        Insert(e.position, e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListen()
            {
                return collection.update.Listen(Process);
            }

            protected override void Refill()
            {
                realIndexes.Clear();
                var coll = collection.current;
                for (int i = 0; i < coll.Count; i++)
                {
                    var item = coll[i];
                    if (predicate(item))
                    {
                        realIndexes.Add(i);
                    }
                }
                buffer.Reset(coll.Where(predicate));
            }
        }

        class ReactiveCollectionFromCellOfArray<T> : AbstractCollectionTransform<T>
        {
            public ICell<IEnumerable<T>> cell;
            protected override IDisposable StartListen()
            {
                return cell.ListenUpdates(coll =>
                {
                    if (coll == null)
                    {
                        buffer.Reset();
                        return;
                    }

                    var newItems = coll as T[] ?? coll.ToArray();
                    for (var index = 0; index < newItems.Length; index++)
                    {
                        var item = newItems[index];
                        if (buffer.Contains(item)) continue;
                        buffer.Add(item);
                    }

                    for (var index = buffer.Count - 1; index >= 0; index--)
                    {
                        var oldItem = buffer[index];
                        if (newItems.Contains(oldItem)) continue;
                        buffer.RemoveAt(index);
                    }
                });
            }

            protected override void Refill()
            {
                buffer.Reset(cell.value);
            }
        }
    }
}