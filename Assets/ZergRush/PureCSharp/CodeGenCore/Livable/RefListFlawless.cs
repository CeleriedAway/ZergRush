using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    /*
     *  This list wraps ids collection into a list of instances got from DataRoot
     *  Done in very simple manner without any instance caching, so every access is data root id dictionary read
     */
    [GenZergRushFolder, GenTask(GenTaskFlags.Serialization | GenTaskFlags.JsonSerialization | GenTaskFlags.CompareChech | GenTaskFlags.UpdateFrom | GenTaskFlags.Hash)]
    public sealed partial class RefListFlawless<T> : IReactiveCollection<T>, IList<T> where T : class, IDataNode, IReferencableFromDataRoot
    {
        List<int> ids = new List<int>();
        
        [GenIgnore] EventStream<ReactiveCollectionEvent<T>> up;
        [GenIgnore] public DataRoot root;
        [GenIgnore] public DataNode carrier;
        [GenIgnore] List<T> __temp = new List<T>();
        
        public void ClearNullsAndLostEntities()
        {
            for (var i = ids.Count - 1; i >= 0; i--)
            {
                if(ids[i] == 0 || root.RecallMayBe(ids[i]) == null) RemoveAt(i);
            }
        }

        List<T> GetCurrent()
        {
            __temp.Clear();
            foreach (var id in ids)
            {
                __temp.Add(root.RecallMayBe<T>(id));
            }
            return __temp;
        }

        public int Count => ids.Count;
        public bool IsReadOnly => false;

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items) Add(item);
        }

        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get { return up = up ?? new EventStream<ReactiveCollectionEvent<T>>(); }
        }

        public bool Contains(T item)
        {
            return ids.Contains(item.Id);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var data = GetCurrent();
            for (int i = arrayIndex; i < data.Count + arrayIndex; i++)
            {
                array[i] = data[i - arrayIndex];
            }
        }

        void OnItemAdd(T item)
        {
            if (item == null) return;
            if (root != null)
            {
                if (root.RecallMayBe(item.Id) != item)
                {
                    throw new ZergRushException(
                        $"this item {item} with id {item.Id} is not contained in any Data/LivableList or Data/LivableSlot");
                }
            }
            if (item.Id == 0)
            {
                throw new ZergRushException($"item {item} added with zero id");
            }
        }

        public void Add(T item)
        {
            ids.Add(item == null ? 0 : item.Id);
            OnItemAdd(item);
            ReactiveCollection<T>.OnItemInserted(item, up, ids.Count - 1);
        }

        public int IndexOf(T item)
        {
            return ids.IndexOf(item == null ? 0 : item.Id);
        }

        public void Insert(int index, T item)
        {
            ids.Insert(index, item == null ? 0 : item.Id);
            OnItemAdd(item);
            ReactiveCollection<T>.OnItemInserted(item, up, index);
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
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
            var data = GetCurrent();
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
            var id = ids[index];
            ids.RemoveAt(index);
            ReactiveCollection<T>.OnItemRemovedAt(index, up, root.RecallMayBe<T>(id));
        }
        
        public void Reset(IEnumerable<T> newDataEnum)
        {
            var newData = newDataEnum.ToList();
            GetCurrent();
            var oldData = GetCurrent();
            ids.Clear();
            for (var i = 0; i < newData.Count; i++)
            {
                var dataNode = newData[i];
                if (dataNode == null) ids[i] = 0;
                else ids[i] = dataNode.Id;
            }
            ReactiveCollection<T>.OnItemsReset(newData, oldData, up);
        }
        
        public int IdAtIndex(int index)
        {
            return ids[index];
        }

        T GetData(int index)
        {
            return root.RecallMayBe<T>(ids[index]);
        }

        public T this[int index]
        {
            get
            {
                return GetData(index);
            }
            set
            {
                var oldItem = this[index];
                OnItemAdd(value);
                ids[index] = value == null ? 0 : value.Id;
                ReactiveCollection<T>.OnItemSet(index, value, oldItem, up);
            }
        }

        public int Capacity
        {
            get { return ids.Capacity; }
            set { ids.Capacity = value; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetCurrent().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return GetCurrent().PrintCollection();
        }

        public void __PropagateHierarchyAndRememberIds()
        {
        }
    }
}