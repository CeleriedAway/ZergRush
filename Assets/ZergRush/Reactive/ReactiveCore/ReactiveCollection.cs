using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace ZergRush.ReactiveCore
{
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
    
    /*
          Reactive collection abstraction.
          Main usecase is presentation of some collections of data in tables.
     */

    public interface IReactiveCollection<out T> : IReadOnlyList<T>
    {
        IEventStream<IReactiveCollectionEvent<T>> update { get; }
    }

    [DebuggerDisplay("{this.ToString()}")]
    public class ReactiveCollection<T> : IReactiveCollection<T>, IList<T>, IConnectable
// Proposition: rename to ReactiveList. Reactive collection is too general. Also I want ReactiveDictionary.
    {
        protected EventStream<ReactiveCollectionEvent<T>> up;
        protected SimpleList<T> data;

        public ReactiveCollection()
        {
            this.data = new SimpleList<T>();
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        public ReactiveCollection(IEnumerable<T> list) : this()
        {
            this.data.AddRange(list);
        }

        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        public bool Contains(T item)
        {
            return data.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = arrayIndex; i < data.Count + arrayIndex; i++)
            {
                array[i] = data[i - arrayIndex];
            }
        }

        public void Add(T item)
        {
            data.Add(item);
            OnItemAdded(item, up, data);
        }
        public static void OnItemAdded(T item, EventStream<ReactiveCollectionEvent<T>> up, IReadOnlyList<T> data)
        {
            OnItemInserted(item, up, data.Count - 1);
        }
        public static void OnItemInserted(T item, EventStream<ReactiveCollectionEvent<T>> up, int index)
        {
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Insert,
                    newItem = item,
                    position = index,
                });
        }

        // wont cause updates
        public ref T AtRef(int index)
        {
            return ref data.AtRef(index);
        }

        public int IndexOf(T item)
        {
            return data.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            data.Insert(index, item);
            OnItemInserted(item, up, index);
        }

        public bool Remove(T item)
        {
            var index = data.IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }
        
        public void Clear()
        {
            Reset(new List<T>());
        }

        public int RemoveAll(Func<T, bool> predicate)
        {
            int removedCounter = 0;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var item = data[i];
                if (predicate(item))
                {
                    removedCounter++;
                    RemoveAt(i);
                }
            }

            return removedCounter;
        }

        public void RemoveRange(int ind, int count)
        {
            for (int i = 0; i < count; i++)
                RemoveAt(ind);
        }

        public void RemoveAt(int index)
        {
            var item = data[index];
            data.RemoveAt(index);
            OnItemRemovedAt(index, up, item);
        }
        
        public void Reset(IReadOnlyList<T> newData)
        {
            var oldData = data;
            data = new SimpleList<T>(newData);
            OnItemsReset(newData, oldData, up);
        }
        
        public static void OnItemRemovedAt(int index, EventStream<ReactiveCollectionEvent<T>> up, T item)
        {
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Remove,
                    position = index,
                    oldItem = item
                });
        }
        
        public void Reset(IEnumerable<T> val = null)
        {
            var oldData = data;
            data = val != null ? new SimpleList<T>(val) : new SimpleList<T>();
            if (oldData.Count == 0 && data.Count == 0) return;

            OnItemsReset(data, oldData, up);
        }

        public T this[int index]
        {
            get { return data[index]; }
            set
            {
                var oldItem = data[index];
                data[index] = value;
                OnItemSet(index, value, oldItem, up);
            }
        }

        public int Capacity
        {
            get { return data.Capacity; }
            set { data.Capacity = value; }
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
            return this.PrintCollection();
        }
        
        public int Count
        {
            get { return data.Count; }
        }

        public bool IsReadOnly { get; }
        public int getConnectionCount => up == null ? 0 : up.getConnectionCount;

        public static void OnItemsReset(IReadOnlyList<T> newData, IReadOnlyList<T> oldData, EventStream<ReactiveCollectionEvent<T>> up)
        {
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Reset,
                    oldData = oldData,
                    newData = newData
                });
        }

        public static void OnItemSet(int index, T newItem, T oldItem, EventStream<ReactiveCollectionEvent<T>> up)
        {
            if (up != null)
            {
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Set,
                    position = index,
                    newItem = newItem,
                    oldItem = oldItem
                });
            }
        }

        // Due to optimization reasons AsCell method send same collection during update process
        // In reality it should copy collection each time
        // So this hack allows this collection to look like new each time and prevent some unexpected behaviour in cases 
        // like coll.AsCell().Map(x => x) not sending update events
        public override bool Equals(object obj)
        {
            return false;
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
                collectionConnection = StartListenAndRefill(); 
            }
            //Debug.Log($"connection counter increased to {connectionCounter} bufferCounter {buffer.connectionCount}");
            connectionCounter++;
        }

        protected abstract IDisposable StartListenAndRefill();

        void OnDisconnect()
        {
            connectionCounter--;
            //Debug.Log($"connection counter decreased to {connectionCounter} bufferCounter {buffer.connectionCount}");
            if (connectionCounter == 0)
            {
                if (buffer.getConnectionCount != 0)
                {
                    //Debug.LogError("WTF is that");
                    throw new Exception("this should not happen");
                }
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
            RefillRaw();
        }
        
        protected abstract void RefillRaw();

        void RefillIfNotConnected()
        {
            if (!connected) RefillBuffer();
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            RefillIfNotConnected();
            return buffer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEventStream<IReactiveCollectionEvent<T>> updateWrapp;

        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get
            {
                return updateWrapp = updateWrapp ?? new AnonymousEventStream<IReactiveCollectionEvent<T>>(act =>
                {
                    OnConnect();
                    var connection = buffer.update.Subscribe(act);
                    return new AnonymousDisposable(() =>
                    {
                        connection.Dispose();
                        OnDisconnect();
                    });
                });
            }
        }

        public override string ToString()
        {
            return this.PrintCollection();
        }

        public int Count
        {
            get
            {
                RefillIfNotConnected();
                return buffer.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                RefillIfNotConnected();
                return buffer[index];
            }
        }
    }

    public class SingleElementCollection<T> : ICell<T>, IReactiveCollection<T>
    {
        //[SerializeField]
        private T val;
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
            return upVal.Subscribe(callback);
        }

        public IDisposable OnChanged(Action action)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Subscribe(_ => action());
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

        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get { return up = up ?? new EventStream<ReactiveCollectionEvent<T>>(); }
        }

        public List<T> current { get {return new List<T>{value};} }
        public int Count => 1;

        public T this[int index] => throw new NotImplementedException();
    }
    
    public class SingleNullableElementCollection<T> : ICell<T>, IReactiveCollection<T> where T : class
    {
        //[SerializeField]
        private T val;
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
            return upVal.Subscribe(callback);
        }

        public IDisposable OnChanged(Action action)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Subscribe(_ => action());
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
            if (value == null) yield break;
            yield return value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (value == null) yield break;
            yield return value;
        }

        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        public List<T> current
        {
            get
            {
                var list = new List<T>();
                if (val != null) list.Add(val);
                return list;
            }
        }

        public int Count => val == null ? 0 : 1;
        public T this[int index] => throw new NotImplementedException();
    }
    
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

        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get { return AbandonedStream<ReactiveCollectionEvent<T>>.value; }
        }

        public List<T> current
        {
            get { return list; }
        }

        static readonly StaticCollection<T> def = new StaticCollection<T>{list = new List<T>()};
        public static IReactiveCollection<T> Empty()
        {
            return def;
        }

        public int Count => list.Count;

        public T this[int index] => list[index];
    }

    public static class ReactiveCollectionExtensions
    {

        public static IReactiveCollection<T> ToStaticReactiveCollection<T>(this List<T> coll)
        {
            return new StaticCollection<T> {list = coll};
        }
        
        public static IReactiveCollection<T> ToStaticReactiveCollection<T>(this IEnumerable<T> coll)
        {
            return new StaticCollection<T> {list = coll.ToList()};
        }

        public static List<T> ToList<T>(this ReactiveCollection<T> coll)
        {
            return coll.AsEnumerable().ToList();
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
        
        public static ICell<bool> ContainsReactive<T>(this IReactiveCollection<T> collection,
            T item)
        {
            return collection.AsCell().Map(c => c.Contains(item));
        }

        public static IReactiveCollection<T> Filter<T>(this IReactiveCollection<T> collection,
            Func<T, bool> predicate)
        {
            return new FilteredCollection<T>(collection, predicate); 
        }
        
        public static IReactiveCollection<T> Filter<T>(this IReactiveCollection<T> collection,
            Func<T, ICell<bool>> predicate)
        {
            return new AdvancedFilteredCollection<T>(collection, predicate); 
        }

        public static IDisposable BindEach<T>(this IReactiveCollection<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
            return collection.update.Subscribe(rce =>
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

        // Wont work well if collection has same elements multiple times
        [MustUseReturnValue]
        public static IDisposable AffectEach<T>(this IReactiveCollection<T> collection, Action<IConnectionSink, T> affect) where T : class 
        {
            var itemConnectionsDict = new Dictionary<T, Connections>();

            collection.BindEach(item => {
                var itemConnections = new Connections();
                if (itemConnectionsDict.ContainsKey(item))
                {
                    UnityEngine.Debug.LogError("it seems item is already loaded, this function wont work if elements repeated in the collection");
                    return;
                }
                affect(itemConnections, item);
                itemConnectionsDict[item] = itemConnections;
            }, item => {
                itemConnectionsDict.TakeKey(item).DisconnectAll();
            });

            return new AnonymousDisposable(() =>
            {
                foreach (var connections in itemConnectionsDict.Values) {
                    connections.DisconnectAll();
                }
            });
        }
        
        public static IDisposable BindEach<T>(this IReactiveCollection<T> collection, Action<T> onInsert, Action<T> onRemove)
        {
            foreach (var item in collection)
            {
                onInsert(item);
            }
            return collection.update.Subscribe(rce =>
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
                    First = connections,
                };
                disposable.Second =
                    // TODO It can be done more effectively then asCell call but much more complex
                    collection.AsCell().Bind(coll =>
                    {
                        connections.DisconnectAll();
                        if (disposable.disposed) return;
                        connections.AddRange(coll.Select(item => item.Subscribe(action)));
                    });
                return disposable;
            });
        }

        public static ICell<T> AtIndex<T>(this IReactiveCollection<T> collection, int index, T ifNoElement = default)
        {
            return new AnonymousCell<T>(action =>
            {
                return collection.AsCell().ListenUpdates(coll =>
                {
                    action(coll.Count > index ? coll[index] : ifNoElement);
                });
            }, () =>
            {
                var coll = collection;
                return coll.Count > index ? coll[index] : ifNoElement;
            });
        }
        
        public static ICell<T> AtIndex<T>(this IReactiveCollection<T> collection, ICell<int> index, T ifNoElement = default)
        {
            return index.FlatMap(v => collection.AtIndex(v, ifNoElement));
        }

        public static ICell<IReadOnlyList<T>> AsCell<T>(this IReactiveCollection<T> collection)
        {
            return new AnonymousCell<IReadOnlyList<T>>(action =>
            {
                return collection.update.Subscribe(_ => { action(collection); });
            }, () => collection);
        }

        public static IReactiveCollection<T> ToReactiveCollection<T>(this ICell<IEnumerable<T>> cell)
        {
            return new ReactiveCollectionFromCellOfArray<T>{cell = cell};    
        }
        
        public static IReactiveCollection<T> ToReactiveCollection<T>(this IReactiveCollection<ICell<T>> coll)
        {
            return new ReactiveCollectionCellJoin<T>{coll = coll};    
        }

        /// <summary>
        /// TODO: Refactor this. Slow, but written fast.
        /// </summary>
        public static IReactiveCollection<T> Reverse<T>(this IReactiveCollection<T> original)
        {
            return original.AsCell().Map(list => list.GetReversed()).ToReactiveCollection();
        }

        public static IReactiveCollection<T> Join<T>(this ICell<IReactiveCollection<T>> cellOfCollection)
        {
            return new JoinCellOfCollection<T> {cellOfCollection = cellOfCollection};
        }
        
        public static IReactiveCollection<T> EnumerateRange<T>(this ICell<int> cellOfElemCount, Func<int, T> fill)
        {
            return new ReactiveRange<T> {fill = fill, cellOfCount = cellOfElemCount};
        }

        [DebuggerDisplay("{this.ToString()}")]
        internal class ConcatCollection<T> : AbstractCollectionTransform<T>
        {
            readonly IReactiveCollection<T> collection;
            readonly IReactiveCollection<T> collection2;
            
            public ConcatCollection(IReactiveCollection<T> collection, IReactiveCollection<T> collection2)
            {
                this.collection = collection;
                this.collection2 = collection2;
            }

            int countFirst => collection.Count;

            void Process(IReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        buffer.RemoveRange(countFirst, buffer.Count - e.oldData.Count);
                        var newDataCount = e.newData.Count;
                        for (var i = 0; i < newDataCount; i++)
                        {
                            buffer.Insert(i, e.newData[i]);
                        }
                        break;
                    case ReactiveCollectionEventType.Insert:
                        buffer.Insert(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        buffer.RemoveAt(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        buffer[e.position] = e.newItem;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            void Process2(IReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        buffer.RemoveRange(countFirst, e.oldData.Count);
                        var newDataCount = e.newData.Count;
                        for (var i = 0; i < newDataCount; i++)
                        {
                            buffer.Add(e.newData[i]);
                        }
                        break;
                    case ReactiveCollectionEventType.Insert:
                        buffer.Insert(e.position + countFirst, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        buffer.RemoveAt(e.position + countFirst);
                        break;
                    case ReactiveCollectionEventType.Set:
                        buffer[e.position + countFirst] = e.newItem;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                RefillBuffer();
                var disp = new DoubleDisposable();
                disp.first = collection.update.Subscribe(Process);
                disp.second = collection2.update.Subscribe(Process2);
                return disp;
            }

            protected override void RefillRaw()
            {
                buffer.Reset(collection);
                buffer.AddRange(collection2);
            }
        }
        
        // cheap version of concat
        public static IReactiveCollection<T> ConcatReactive<T>(this IReactiveCollection<T> collection, IReactiveCollection<T> collection2)
        {
            return new ConcatCollection<T>(collection, collection2);
        }

        class ReactiveRange<T> : AbstractCollectionTransform<T>
        {
            public Func<int, T> fill;
            public ICell<int> cellOfCount;
            protected override IDisposable StartListenAndRefill()
            {
                return cellOfCount.Bind(FillBuffer);
            }
            
            protected override void RefillRaw()
            {
                FillBuffer(cellOfCount.value);
//                for (int i = 0; i < cellOfCount.value; i++)
//                {
//                    buffer.Add(fill(i));
//                }
            }

            void FillBuffer(int i)
            {
                while (buffer.Count != i)
                {
                    if (buffer.Count > i) buffer.RemoveAt(buffer.Count - 1);
                    else if (buffer.Count < i) buffer.Add(fill(buffer.Count));
                }
            }

        }

        class JoinCellOfCollection<T> : IReactiveCollection<T>
        {
            public ICell<IReactiveCollection<T>> cellOfCollection;
            public IEnumerator<T> GetEnumerator()
            {
                return cellOfCollection.value.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEventStream<IReactiveCollectionEvent<T>> update
            {
                get
                {
                    var cellUpdates = cellOfCollection.BufferPreviousValue().Map(tuple => new ReactiveCollectionEvent<T>
                    {
                        type = ReactiveCollectionEventType.Reset,
                        newData = tuple.Item1,
                        oldData = tuple.Item2
                    });
                    return cellOfCollection.Map(coll => coll.update).Join().MergeWith(cellUpdates);
                }
            }

            public int Count => cellOfCollection.value.Count;

            public T this[int index] => cellOfCollection.value[index];
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

            void Process(IReactiveCollectionEvent<T> e)
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

            protected override IDisposable StartListenAndRefill()
            {
                RefillBuffer();
                return collection.update.Subscribe(Process);
            }

            protected override void RefillRaw()
            {
                buffer.Reset(collection.Select(mapFunc));
            }
        }
        
        // WIP
        [DebuggerDisplay("{this.ToString()}")]
        public class MergedCollection<T> : AbstractCollectionTransform<T>
        {
            public MergedCollection(IReactiveCollection<T> []collections)
            {
                this.collections = collections;
            }

            readonly IReactiveCollection<T> [] collections;


            void Process(ReactiveCollectionEvent<T> e, int index)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        ProperRefill();
                        break;
                    case ReactiveCollectionEventType.Insert:
//                        Insert(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
//                        Remove(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        //TODO make proper set event resolve if needed
//                        Remove(e.position); 
//                        Insert(e.position, e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void Remove(int ePosition, bool second)
            {
            }

            void Insert(int ePosition, T eNewItem, bool second)
            {
            }

            void ProperRefill()
            {
                buffer.Clear();
            }

            protected override IDisposable StartListenAndRefill()
            {
                throw new NotImplementedException();
//                ProperRefill();
//                return new DoubleDisposable{first = collection2.update.Subscribe(Process), second = collection1.update.Subscribe(Process)};
            }

            protected override void RefillRaw()
            {
//                buffer.Reset(collection1.current);
//                buffer.AddRange(collection2.current);
            }
        }
        
        [DebuggerDisplay("{this.ToString()}")]
        public class AdvancedFilteredCollection<T> : AbstractCollectionTransform<T>
        {
            readonly Func<T, ICell<bool>> predicate;
            readonly IReactiveCollection<T> collection;
            Connections connetions = new Connections();
            List<int> realIndexes = new List<int>();
            
            public AdvancedFilteredCollection(IReactiveCollection<T> collection, Func<T, ICell<bool>> predicate)
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

                void InsertInner()
                {
                    var i = realIndexes.UpperBound(realIndex);
                    realIndexes.Insert(i, realIndex);
                    buffer.Insert(i, item);
                }

                var passCell = predicate(item);
                if (passCell.value)
                {
                    InsertInner();
                }

                connetions.Insert(realIndex, passCell.ListenUpdates(pass => {
                    if (pass)
                    {
                        InsertInner();
                    }
                    else
                    {
                        var i = realIndexes.IndexOf(realIndex);
                        if (i == -1) throw new ZergRushException("this real index must be here");
                        realIndexes.RemoveAt(i);
                        buffer.RemoveAt(i);
                    }
                }));
            }

            void Remove(int realIndex)
            {
                if (realIndexes.Count == 0) return;
                var oldIndex = realIndexes.BinarySearch(realIndex);
                for (var i = oldIndex >= 0 ? oldIndex : ~oldIndex; i < realIndexes.Count; ++i)
                {
                    realIndexes[i]--;
                }
                connetions.TakeAt(realIndex).Dispose();
                if (oldIndex < 0) return;
                realIndexes.RemoveAt(oldIndex);
                buffer.RemoveAt(oldIndex);
            }

            void Process(IReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        ProperRefill();
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

            void ProperRefill()
            {
                buffer.Clear();
                connetions.DisconnectAll();
                realIndexes.Clear();
                
                var coll = collection;
                for (int i = 0; i < coll.Count; i++)
                {
                    var item = coll[i];
                    Insert(i, item);
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                ProperRefill();
                return new DoubleDisposable{first = connetions, second = collection.update.Subscribe(Process)};
            }

            protected override void RefillRaw()
            {
                buffer.Reset(collection.Where(i => predicate(i).value));
            }
        }
        
        class ReactiveCollectionCellJoin<T> : AbstractCollectionTransform<T>
        {
            public IReactiveCollection<ICell<T>> coll;

            Connections connetions = new Connections();
            
            void Insert(int realIndex, ICell<T> item)
            {
                buffer.Insert(realIndex, item.value);
                connetions.Insert(realIndex, item.ListenUpdates(val =>
                {
                    buffer[coll.IndexOf(item)] = val;
                }));
            }

            void Remove(int realIndex)
            {
                connetions.TakeAt(realIndex).Dispose();
                buffer.RemoveAt(realIndex);
            }
            
            void Set(int realIndex, ICell<T> item)
            {
                connetions[realIndex].Dispose();
                connetions[realIndex] = item.ListenUpdates(val =>
                {
                    buffer[coll.IndexOf(item)] = val;
                });
                buffer[realIndex] = item.value;
            }

            void ProperRefill(IReadOnlyList<ICell<T>> newList)
            {
                connetions.DisconnectAll();
                for (int i = 0; i < newList.Count; i++)
                {
                    var cell = newList[i];
                    connetions.Add(cell.ListenUpdates(val =>
                    {
                        buffer[coll.IndexOf(cell)] = val;
                    }));
                }
                buffer.Reset(newList.Select(l => l.value));
            }

            void Process(IReactiveCollectionEvent<ICell<T>> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        ProperRefill(e.newData);
                        break;
                    case ReactiveCollectionEventType.Insert:
                        Insert(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        Remove(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        Set(e.position, e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                ProperRefill(coll);
                return new DoubleDisposable
                {
                    first = connetions,
                    second = coll.update.Subscribe(Process)
                };
            }

            protected override void RefillRaw()
            {
                buffer.Clear();
                foreach (var cell in coll)
                {
                    buffer.Add(cell.value);
                }
            }
        }
        
        [DebuggerDisplay("{this.ToString()}")]
        public class SortedCollection<T> : AbstractCollectionTransform<T>
        {
            public readonly Func<T, T, int> sorter;
            public readonly IReactiveCollection<T> collection;
            
            public SortedCollection(IReactiveCollection<T> collection, Func<T, T, int> predicate)
            {
                this.collection = collection;
                this.sorter = predicate;
            }

            void Insert(T item)
            {
                buffer.InsertSorted(sorter, item);
            }

            void Remove(T oldItem)
            {
                buffer.Remove(oldItem);
            }

            void Process(IReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        RefillRaw();
                        break;
                    case ReactiveCollectionEventType.Insert:
                        Insert(e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        Remove(e.oldItem);
                        break;
                    case ReactiveCollectionEventType.Set:
                        //TODO make proper set event resolve if needed
                        Remove(e.oldItem); 
                        Insert(e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            protected override IDisposable StartListenAndRefill()
            {
                RefillRaw();
                return collection.update.Subscribe(Process);
            }

            protected override void RefillRaw()
            {
                buffer.Clear();
                var coll = collection;
                for (int i = 0; i < coll.Count; i++)
                {
                    var item = coll[i];
                    buffer.InsertSorted(sorter, item);
                }
            }
        }

        public static IReactiveCollection<T> SortReactive<T>(this ReactiveCollection<T> collection,
            Func<T, T, int> comparator)
        {
            return new SortedCollection<T>(collection, comparator);
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

            void Process(IReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        ProperRefill();
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

            void ProperRefill()
            {
                buffer.Clear();
                realIndexes.Clear();
                var coll = collection;
                for (int i = 0; i < coll.Count; i++)
                {
                    var item = coll[i];
                    if (predicate(item))
                    {
                        realIndexes.Add(i);
                        buffer.Add(item);
                    }
                }
            }
            protected override IDisposable StartListenAndRefill()
            {
                ProperRefill();
                return collection.update.Subscribe(Process);
            }

            protected override void RefillRaw()
            {
                buffer.Reset(collection.Where(predicate));
            }
        }


        class ReactiveCollectionFromCellOfArray<T> : AbstractCollectionTransform<T>
        {
            public ICell<IEnumerable<T>> cell;
            protected override IDisposable StartListenAndRefill()
            {
                return cell.Bind(coll =>
                {
                    if (coll == null)
                    {
                        buffer.Reset();
                        return;
                    }

                    // TODO make smarter algorithm later
                    buffer.Reset(coll);
                    
                    // This algorithm does not work on simple types and same items in collection
//                    var newItems = coll as T[] ?? coll.ToArray();
//                    for (var index = 0; index < newItems.Length; index++)
//                    {
//                        var item = newItems[index];
//                        if (buffer.Contains(item)) continue;
//                        buffer.Add(item);
//                    }
//
//                    for (var index = buffer.Count - 1; index >= 0; index--)
//                    {
//                        var oldItem = buffer[index];
//                        if (newItems.Contains(oldItem)) continue;
//                        buffer.RemoveAt(index);
//                    }
                });
            }

            protected override void RefillRaw()
            {
                buffer.Reset(cell.value);
            }
        }
        
        class ReactiveCollectionFromCellOfCollection<T> : AbstractCollectionTransform<T>
        {
            public ICell<IReactiveCollection<T>> cell;
            protected override IDisposable StartListenAndRefill()
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

            protected override void RefillRaw()
            {
                buffer.Reset(cell.value);
            }
        }
    }
}