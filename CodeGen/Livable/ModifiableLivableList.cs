using UnityEngine;

namespace ZergRush.Alive
{
    public partial class ModifiableLivableList<T> : LivableList<T>, IStaticallyModifiable, IReferencableFromDataRoot where T : Livable, IReferencableFromDataRoot
    {
        public int id;
        public int Id { get { return id; } set { id = value; root?.ForceId(value, this); } }

        public void ModifyAddInstance(StaticConnections connections, T item)
        {
            item.root = root;
            item.carrier = carrier;
            item.__PropagateHierarchyAndRememberIds();
            item.__parent_id = connections.ownerId;
            if (item.Id == 0)
            {
                item.__GenIds(root);
            }
            Add(item);
            connections.Add(new SerializableConnection(id, item.Id));
        }
        public new void __GenIds(DataRoot __root)
        {
            Id = __root.entityIdFactory++;
            base.__GenIds(__root);
        }
        public new void __PropagateHierarchyAndRememberIds() 
        {
            //Debug.Log($"{root.__debugTag} livable list setup id:{Id}");
            if (Id != 0) root.Remember(this, Id);
            base.__PropagateHierarchyAndRememberIds();
        }
        public new void __ForgetIds() 
        {
            if (Id != 0) root.Forget(Id, this);
            base.__ForgetIds();
        }

        public void DisposeAffect(int itemId)
        {
            foreach (var item in this)
            {
                if (item.Id == itemId)
                {
                    Remove(item);
                    break;
                }
            }
        }
        
    }
}