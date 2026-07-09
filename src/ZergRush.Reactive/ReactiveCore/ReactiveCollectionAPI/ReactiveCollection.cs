using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace ZergRush.ReactiveCore
{
    [DebuggerDisplay("{this.ToString()}")]
    public class ReactiveCollection<T> : IReactiveCollection<T>, IList<T>, IConnectable
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

        /// wont cause updates
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
            ResetConsumeList(new SimpleList<T>());
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
        
        // directly takes this list, this is just an optimization for some cases
        public void ResetConsumeList(SimpleList<T> list)
        {
            var oldData = data;
            data = list;
            OnItemsReset(list, oldData, up);
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

        public static void OnItemsReset(IReadOnlyList<T> newData, IReadOnlyList<T> oldData,
            EventStream<ReactiveCollectionEvent<T>> up)
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
            if (up != null && EqualityComparer<T>.Default.Equals(newItem, oldItem) == false)
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

        /// Due to optimization reasons AsCell method send same collection during update process
        /// In reality it should copy collection each time
        /// So this hack allows this collection to look like new each time and prevent some unexpected behaviour in cases 
        /// like coll.AsCell().Map(x => x) not sending update events
        public override bool Equals(object obj)
        {
            return false;
        }
    }
}