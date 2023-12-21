using System;
using System.Diagnostics;
using ZergRush.Alive;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    [DebuggerDisplay("{this.ToString()}")]
    public partial class LivableList<T> : DataList<T>, IAddCopyList<T> where T : Livable, new()
    {
        [GenIgnore] bool alive;

        public void Enlive()
        {
            if (alive)
            {
                throw new ZergRushException("You can not enlive living");
            }

            alive = true;
            //Debug.Log(items.PrintCollection());
            for (var i = 0; i < items.Count; i++)
            {
                //Debug.Log($"enliving {items[i]}" );
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

        protected override void ProcessRemoveItem(T item)
        {
            if (alive)
            {
                item?.Mortify();
            }
            base.ProcessRemoveItem(item);
            if (root != null && root.pool != null) item?.ReturnToPool(root.pool);
        }
        
        public void InsertCopy(T item, T refData, ZRUpdateFromHelper __helper, int index)
        {
            if (refData == null)
            {
                items.Add(null);
                return;
            }

            bool updated = refData is IsMultiRef ? __helper.TryLoadAlreadyUpdated(refData, ref item) : false;
            
            items.Insert(index, item);
            SetupItemHierarchy(item);

            if (!updated && refData != null)
                item?.UpdateFrom(refData, __helper);
            
            if (alive)
                item?.Enlive();
            
            ReactiveCollection<T>.OnItemInserted(item, up, index);
        }

        public new void AddCopy(T item, T refData, ZRUpdateFromHelper __helper)
        {
            InsertCopy(item, refData, __helper, items.Count);
        }

        protected override void ProcessAddItem(T item)
        {
            base.ProcessAddItem(item);
            if (alive) {
                item?.Enlive();
            }
        }

        public void OnReturnToPool(ObjectPool pool)
        {
            if (alive)
            {
                throw new ZergRushException($"this method should not be called on alive list");
            }

            foreach (var item in items)
            {
                item?.ReturnToPool(pool);
            }

            items.Clear();
        }


        public override string ToString()
        {
            return this.PrintCollection();
        }
    }
}