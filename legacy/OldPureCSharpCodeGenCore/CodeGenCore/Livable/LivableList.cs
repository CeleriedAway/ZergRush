using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    // tag interface for some impl details
    public interface ILivableContainer {}
    public interface __ISwappable { void SwapItem(int i, int j); }
    
    [DebuggerDisplay("{this.ToString()}")]
    public partial class LivableList<T> : IList<T>, IReadOnlyList<T>, IReactiveCollection<T>, IConnectable,
        IAddCopyList<T>, __ISwappable, ILivableContainer where T : Livable
    {
        public bool __update_mod;
        protected EventStream<ReactiveCollectionEvent<T>> up;
        [GenIgnore] bool alive;

        [GenIgnore] public LivableRoot root;
        [GenIgnore] public Livable carrier;
        [GenIgnore] public EventStream<T> removed = new EventStream<T>();
        
        protected List<T> items = new List<T>();
        
        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        public int Capacity
        {
            get => items.Capacity;
            set
            {
                if (value > items.Capacity)
                    items.Capacity = value;
            }
        }
        
        public void SetRootAndCarrier(LivableRoot root, Livable carrier)
        {
            this.root = root;
            this.carrier = carrier;
        }
        
        public void ForEach(Action<T> action)
        {
            for (var i = 0; i < Count; i++)
            {
                var val = this[i];
                action(val);
            }
        }

        public List<T> GetFiltered(Func<T, bool> filter) => items.Filter(filter);

        protected void SetupItemHierarchy(T item)
        {
            if (item == null) return;
            item.SetRootAndCarrier(root, carrier);
            item.__PropagateHierarchy();
        }

        protected virtual void ProcessAddItem(T item)
        {
            if (item == null) return;
            SetupItemHierarchy(item);
            if (!__update_mod)
            {
                item.OnInsertedIntoHierarchy();
            }
            if (alive)
            {
                item.Enlive();
            }
        }
        
        protected virtual void ProcessRemoveItem(T item)
        {
            if (item == null) return;
            if (alive)
            {
                item.Mortify();
            }
            if (!__update_mod)
            {
                item.Destroy();
            }
            if (root != null && root.pool != null) item.ReturnToPool(root.pool);
        }
        
        public void Enlive()
        {
            if (alive)
            {
                throw new ZergRushException("You can not enlive living");
            }

            alive = true;
            for (var i = 0; i < items.Count; i++)
            {
                items[i]?.Enlive();
            }
        }

        public void Mortify()
        {
            if (!alive)
            {
                throw new ZergRushException("You can not mortify dead");
            }

            for (var i = 0; i < items.Count; i++)
            {
                items[i]?.Mortify();
            }

            alive = false;
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
            ReactiveCollection<T>.OnItemInserted(item, up, items.Count - 1);
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
            var didRemove = index != -1;                
            if (didRemove)
                RemoveAt(index);
            return didRemove;
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
        
        public void InsertCopy(T item, T refData, ZRUpdateFromHelper __helper, int index)
        {
            if (refData == null)
            {
                items.Insert(index, null);
                return;
            }

            bool updated = refData is IsMultiRef ? __helper.TryLoadAlreadyUpdatedLivable(refData, ref item, true) : false;
            
            items.Insert(index, item);
            SetupItemHierarchy(item);

            if (!updated)
                item?.UpdateFrom(refData, __helper);
            
            if (alive)
                item?.Enlive();
            
            ReactiveCollection<T>.OnItemInserted(item, up, index);
        }

        public void AddCopy(T item, T refData, ZRUpdateFromHelper __helper)
        {
            InsertCopy(item, refData, __helper, items.Count);
        }

        public void __PropagateHierarchy()
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;
                item.SetRootAndCarrier(root, carrier);
                item.__PropagateHierarchy();
            }
        }

        public void VisitNode(Action<object> action)
        {
            foreach (var item in items)
            {
                if (item == null) continue;
                item.VisitNode(action);
            }
        }

        public void OnReturnToPool(ObjectPool pool)
        {
            if (alive)
            {
                throw new ZergRushException("this method should not be called on alive list");
            }

            foreach (var item in items)
            {
                item?.ReturnToPool(pool);
            }

            items.Clear();
        }
        
        public void SwapItem(int i, int j)
        {
            (items[i], items[j]) = (items[j], items[i]);
        }

        public override string ToString()
        {
            return this.PrintCollection();
        }
    }
}
