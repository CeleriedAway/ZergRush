using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Utilities;
using UnityEngine;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    public interface INeedUpdateFromPostProcess
    {
        void OnUpdateFinished();
    }
    
    [GenInLocalFolder, GenTask(GenTaskFlags.Serialization | GenTaskFlags.JsonSerialization), GenTaskCustomImpl(GenTaskFlags.CompareChech | GenTaskFlags.UpdateFrom | GenTaskFlags.Hash)]
    public sealed partial class RefListMk2<T> : IReactiveCollection<T>, IList<T>, INeedUpdateFromPostProcess where T : class, IDataNode, IReferencableFromDataRoot
    {
        [GenIgnore]
        List<T> data;
        [GenIgnore] RefListMk2<T> mirroringList;
        List<int> ids = new List<int>();

        public DataRoot root
        {
            get { return _root; }
            set
            {
                _root = value;
            }
        }
        
        public void ClearDead()
        {
            for (var i = ids.Count - 1; i >= 0; i--)
            {
                if(ids[i] == 0 || root.RecallMayBe(ids[i]) == null) RemoveAt(i);
            }
        }

        [GenIgnore] bool isSetUp;

        void Invalidate() => isSetUp = false;

        void CheckSetup()
        {
            if (isSetUp) return;
            isSetUp = true;
            Setup();
        }
        
        void Setup()
        {
            if (root is LivableRoot lr && !lr.isAlive)
            {
                isSetUp = false;
                return;
            }
            data = new List<T>(ids.Count);
            data.Capacity = ids.Count;
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (id == 0)
                {
                    data.Add(null);
                    continue;
                }
                
                var entity = _root.RecallMayBe(id);
                if (entity == null)
                {
                    Debug.Log($"recalling id not found {id}");
                    data.Add(null);
                }
                else if (entity is T t)
                {
                    data.Add(t);
                    OnItemAdd(t);
                }
                else
                {
                    Debug.LogError($"entity recalled is other type, expected:{typeof(T)} got:{entity.GetType()}");
                    data.Add(null);
                }
            }
            //up?.Send(new ReactiveCollectionEvent<T>{type = ReactiveCollectionEventType.Reset, newData = data, oldData = data});
        }
        
        [GenIgnore] public DataRoot _root;
        [GenIgnore] public DataNode carrier;

        [GenIgnore]
        EventStream<ReactiveCollectionEvent<T>> up;
        public int Count => ids.Count;
        public bool IsReadOnly => false;

        public List<T> current
        {
            get { CheckSetup(); return data; }
            set { Reset(value); }
        }

        public RefListMk2()
        {
            this.data = new List<T>();
        }

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
            CheckSetup();
            return data.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            CheckSetup();
            for (int i = arrayIndex; i < data.Count + arrayIndex; i++)
            {
                array[i] = data[i - arrayIndex];
            }
        }

        void OnItemAdd(T item)
        {
            if (item == null) return;

            if (item.Id == 0)
            {
                throw new ZergRushException($"item {item} added with zero id");
            }
            
            //Debug.Log($"Adding ref item:{item} id:{item?.Id}");
            item.destroyEvent.Subscribe(() =>
            {
                //Debug.Log($"Removing ref from ref list:{item} id:{item?.Id}");
                // var i = ids.IndexOf(id);
                // if (i != -1)
                // {
                //     data.RemoveAt(i);
                //     ids.RemoveAt(i);
                //     ReactiveCollection<T>.OnItemRemovedAt(i, up, item);
                // }
                //Debug.Log($"removing destroyed item:{item} id:{item.Id}");
                Remove(item);
            });
        }

        void OnItemRemoved(T item)
        {
            //so we need to store subscribtion for proper unsubscribe,
            //but if we not we'll have multiple removes which is not that bad for now
        }

        public void Add(T item)
        {
            CheckSetup();
            ids.Add(item == null ? 0 : item.Id);
            data.Add(item);
            OnItemAdd(item);
            ReactiveCollection<T>.OnItemAdded(item, up, data);
        }

        public int IndexOf(T item)
        {
            CheckSetup();
            return data.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            data.Insert(index, item);
            ids.Insert(index, item == null ? 0 : item.Id);
            OnItemAdd(item);
            ReactiveCollection<T>.OnItemInserted(item, up, index);
        }

        public bool Remove(T item)
        {
            CheckSetup();
            var index = data.IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            else
            {
                // TODO some desubscribsion process is required so items wont be removed multiple times
                // for now this can happen
                // Debug.LogError($"item not found {item}");
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
            ids.RemoveAt(index);
            OnItemRemoved(item);
            ReactiveCollection<T>.OnItemRemovedAt(index, up, item);
        }
        
        public void Reset(IEnumerable<T> newDataEnum)
        {
            var newData = newDataEnum.ToList();
            var oldData = data;
            data = newData;
            ids.Resize(data.Count);
            for (var i = 0; i < data.Count; i++)
            {
                var dataNode = data[i];
                ids[i] = dataNode?.Id ?? 0;
            }

            ReactiveCollection<T>.OnItemsReset(newData, oldData, up);
        }
        
        public int IdAtIndex(int index)
        {
            return ids[index];
        }

        T GetData(int index)
        {
            if (data[index] == null)
            {
                data[index] = _root.RecallMayBe(ids[index]) as T;
            }
            return data[index];
        }

        public T this[int index]
        {
            get
            {
                CheckSetup();
                return GetData(index);
            }
            set
            {
                CheckSetup();
                var oldItem = data[index];
                data[index] = value;
                ids[index] = value == null ? 0 : value.Id;
                OnItemAdd(value);
                ReactiveCollection<T>.OnItemSet(index, value, oldItem, up);
            }
        }

        public int Capacity
        {
            get { return data.Capacity; }
            set { data.Capacity = value; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            CheckSetup();
            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            CheckSetup();
            return GetEnumerator();
        }

        public override string ToString()
        {
            CheckSetup();
            return this.PrintCollection();
        }
        
        public int getConnectionCount => up == null ? 0 : up.getConnectionCount;

        public void UpdateFrom(RefListMk2<T> other)
        {
            if (root == null)
            {
                isSetUp = false;
                ids.Clear();
                ids.AddRange(other.ids);
                data.Clear();
            }
            else
            {
                other.CheckSetup();
                mirroringList = other;
                if (mirroringList.data.Count != mirroringList.ids.Count)
                {
                    Debug.LogError("asdfadsf");
                }
                root.__RegisterUpdatePostprocess(this);
            }
        }
        
        public void OnUpdateFinished()
        {
            if (ids.Count == mirroringList.ids.Count)
            {
                for (var i = 0; i < ids.Count; i++)
                {
                    if (ids[i] != mirroringList.ids[i]) break;
                }
                // all ids are same so no need to do anything
                return;
            }
            ids.Clear();
            var oldData = data.ToList();
            data.Clear();
            ids.AddRange(mirroringList.ids);
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (i >= mirroringList.data.Count || i < 0)
                {
                    Debug.LogError($"asdf {mirroringList.GetHashCode()} {this.GetHashCode()} {i} {mirroringList.data.Count}");
                    break;
                }
                if (id == 0 || mirroringList.data[i] == null) data.Add(null);
                else
                {
                    var e = root.RecallMayBe<T>(id);
                    if (e == null)
                    {
                        Debug.LogError($"entity form {this} with id:{id} data:{mirroringList.data.PrintCollection()}");
                    }
                    data.Add(e);
                }
            }

            ReactiveCollection<T>.OnItemsReset(data, oldData, up);
        }


        public void CompareCheck(RefListMk2<T> other, Stack<string> path)
        {
            if (Count != other.Count) SerializationTools.LogCompError(path, "Count", other.Count, Count);
            var count = Math.Min(Count, other.Count);
            for (int i = 0; i < count; i++)
            {
                if (ids[i] != other.ids[i]) SerializationTools.LogCompError(path, $"id at index: {i.ToString()}", ids[i], other.ids[i]);
            }
        }
        
        public ulong CalculateHash()
        {
            ulong hash = 0xffffff;
            for (int i = 0; i < Count; i++)
            {
                hash += (ulong)ids[i];
                hash += hash << 11; hash ^= hash >> 7;
            }
            return hash;
        }

        public void __PropagateHierarchyAndRememberIds()
        {
        }
    }
}