using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
        public T item;
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
                    item = item,
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
                    item = item,
                    position = index
                });
        }

        public void Remove(T item)
        {
            RemoveAt(data.IndexOf(item));
        }

        public void RemoveAt(int index)
        {
            data.RemoveAt(index);
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Remove,
                    position = index
                });
        }

        public void Reset(List<T> newData)
        {
            data = newData;
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Reset,
                });
        }

        public void Reset(IEnumerable<T> val = null)
        {
            data.Clear();
            if (val != null)
                data.AddRange(val);
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Reset,
                });
        }

        public T this[int index]
        {
            get { return data[index]; }
            set
            {
                data[index] = value;
                if (up != null)
                    up.Send(new ReactiveCollectionEvent<T>
                    {
                        type = ReactiveCollectionEventType.Set,
                        position = index,
                        item = value
                    });
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
        
        protected EventStream<ReactiveCollectionEvent<T>> up = new EventStream<ReactiveCollectionEvent<T>>();
        protected readonly List<T> buffer = new List<T>();
        
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
            buffer.Clear();
        }

        protected void RefillBuffer()
        {
            buffer.Clear();
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
                    if (up == null) up = new EventStream<ReactiveCollectionEvent<T>>();
                    var connection = up.Listen(act);
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
                if (connected) return buffer;
                RefillBuffer();
                return buffer;
            }
        }

        public override string ToString()
        {
            return current.PrintCollection();
        }
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
                    up.Send(new ReactiveCollectionEvent<TMapped>
                    {
                        type = ReactiveCollectionEventType.Reset
                    });
                    break;
                case ReactiveCollectionEventType.Insert:
                    var item = mapFunc(e.item);
                    buffer.Insert(e.position, item);
                    up.Send(new ReactiveCollectionEvent<TMapped>
                    {
                        type = ReactiveCollectionEventType.Insert,
                        item = item,
                        position = e.position
                    });
                    break;
                case ReactiveCollectionEventType.Remove:
                    buffer.RemoveAt(e.position);
                    up.Send(new ReactiveCollectionEvent<TMapped>
                    {
                        type = ReactiveCollectionEventType.Remove,
                        position = e.position
                    });
                    break;
                case ReactiveCollectionEventType.Set:
                    var newItem = mapFunc(e.item);
                    buffer[e.position] = newItem;
                    up.Send(new ReactiveCollectionEvent<TMapped>
                    {
                        type = ReactiveCollectionEventType.Set,
                        item = newItem,
                        position = e.position
                    });
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
            buffer.AddRange(collection.current.Select(mapFunc));
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
            up.Send(new ReactiveCollectionEvent<T>
            {
                type = ReactiveCollectionEventType.Insert,
                item = item,
                position = newIndex
            });
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
            up.Send(new ReactiveCollectionEvent<T>
            {
                type = ReactiveCollectionEventType.Remove,
                position = oldIndex
            });
        }

        void Process(ReactiveCollectionEvent<T> e)
        {
            switch (e.type)
            {
                case ReactiveCollectionEventType.Reset:
                    RefillBuffer();
                    up.Send(new ReactiveCollectionEvent<T> {
                        type = ReactiveCollectionEventType.Reset
                    });
                    break;
                case ReactiveCollectionEventType.Insert:
                    Insert(e.position, e.item);
                    break;
                case ReactiveCollectionEventType.Remove:
                    Remove(e.position);
                    break;
                case ReactiveCollectionEventType.Set:
                    //TODO make proper set event resolve if needed
                    Remove(e.position); 
                    Insert(e.position, e.item);
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
            var coll= collection.current;
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
                        action(rce.item);
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
    }
}