using System;
using System.Collections;
using System.Collections.Generic;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    public partial class DataList<T> : IList<T>, IReadOnlyList<T>
        , IReactiveCollection<T>, IConnectable
        where T : DataNode

    {
        public bool __update_mod;
        protected EventStream<ReactiveCollectionEvent<T>> up;
        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        [GenIgnore] public DataRoot root;
        [GenIgnore] public DataNode carrier;
        [GenIgnore] public EventStream<T> removed = new EventStream<T>();
        
        protected List<T> items = new List<T>();
        
        public int Capacity
        {
            get => items.Capacity;
            set => items.Capacity = value;
        }
        
        public void ForEach(Action<T> action)
        {
            for (var i = 0; i < this.Count; i++)
            {
                var val = this[i];
                action(val);
            }
        }

        public List<T> GetFiltered(Func<T, bool> filter) => items.Filter(filter);

        protected void SetupItemHierarchy(T item)
        {
            item.carrier = carrier;
            item.root = root;
            item.__PropagateHierarchyAndRememberIds();
        }
        protected virtual void ProcessAddItem(T item)
        {
            SetupItemHierarchy(item);
            if (!__update_mod)
            {
                if (item.staticConnections.ownerId == 0)
                {
                    var hasId = this as IReferencableFromDataRoot;
                    item.staticConnections.ownerId = hasId != null ? hasId.Id : -1;
                }
                item.OnInsertedIntoHierarchy(item.staticConnections);
            }
        }
        
        protected virtual void ProcessRemoveItem(T item)
        {
            item.__ForgetIds();
            if (!__update_mod)
            {
                item.Destroy();
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            items.Add(item);
            if (item != null) ProcessAddItem(item);
            ReactiveCollection<T>.OnItemAdded(item, up, items);
        }

        public void Clear()
        {
            foreach (var item in items)
            {
                ProcessRemoveItem(item);
            }
            var oldItems = items;
            items = new List<T>();
            ReactiveCollection<T>.OnItemsReset(items, oldItems, up);
        }

        public bool Contains(T item)
        {
            return items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            int index = items.IndexOf(item);
            var removed = index != -1;                
            if (removed)
                RemoveAt(index);
            return removed;
        }

        public int Count => items.Count;
        public bool IsReadOnly => false;
        public int IndexOf(T item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            items.Insert(index, item);
            if (item != null)
                ProcessAddItem(item);
            ReactiveCollection<T>.OnItemInserted(item, up, index);
        }

        public void RemoveAt(int index)
        {
            var item = items[index];
            ProcessRemoveItem(item);
            items.RemoveAt(index);
            removed.Send(item);
            ReactiveCollection<T>.OnItemRemovedAt(index, up, item);
        }

        public T this[int index]
        {
            get { return items[index]; }
            set
            {
                var currItem = items[index];
                if (ReferenceEquals(value, currItem)) return;
                
                if (currItem != null)
                    ProcessRemoveItem(currItem);
                items[index] = value;
                if (value != null) ProcessAddItem(value);
                ReactiveCollection<T>.OnItemSet(index, value, currItem, up);
            }
        }

        public int getConnectionCount => up != null ? up.getConnectionCount : 0;
        
        public void AddCopy(T item, T refData)
        {
            items.Add(item);
            SetupItemHierarchy(item);
            item.UpdateFrom(refData);
            ReactiveCollection<T>.OnItemAdded(item, up, items);
        }


        public void __GenIds(DataRoot __root)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.__GenIds(__root);
            }
        }

        public void __PropagateHierarchyAndRememberIds()
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.root = root;
                item.carrier = carrier;
                item.__PropagateHierarchyAndRememberIds();
            }
        }

        public void __ForgetIds()
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.__ForgetIds();
            }
        }
    }
}