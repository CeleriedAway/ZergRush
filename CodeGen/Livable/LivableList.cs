using System;
using ZergRush.Alive;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    public partial class LivableList<T> : DataList<T> where T : Livable
    {
        [GenIgnore] bool alive;

        public void Enlive()
        {
            if (alive)
            {
                throw new Exception("You can not enlive living");
            }

            alive = true;
            //Debug.Log(items.PrintCollection());
            for (var i = 0; i < items.Count; i++)
            {
                //Debug.Log($"enliving {items[i]}" );
                items[i].Enlive();
            }
        }

        public void Mortify()
        {
            if (!alive)
            {
                throw new Exception("You can not mortify dead");
            }

            for (var i = 0; i < items.Count; i++)
            {
                items[i].Mortify();
            }

            alive = false;
        }

        protected override void ProcessRemoveItem(T item)
        {
            if (alive)
            {
                item.Mortify();
            }
            base.ProcessRemoveItem(item);
            if (root != null && root.pool != null) item.ReturnToPool(root.pool);
        }

        public new void AddCopy(T item, T refData)
        {
            items.Add(item);
            SetupItemHierarchy(item);
            item.UpdateFrom(refData);
            if (alive) {
                item.Enlive();
            }
            ReactiveCollection<T>.OnItemAdded(item, up, items);
        }

        protected override void ProcessAddItem(T item)
        {
            base.ProcessAddItem(item);
            if (alive) {
                item.Enlive();
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
                item.ReturnToPool(pool);
            }

            items.Clear();
        }
    }
}