using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using ZergRush.Alive;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    /*
     *     Base class for data node that knows about hierarchy it's being contained in
     *     Also it could be marked as destroyed for containers like DataList to delete it
     *     Data node can have reference id to be referenced from other parts of data tree
     *     To make it referensable add "int id;" field and [HasRefId] tag on the class
     */
    [GenTask(GenTaskFlags.NodePack & ~GenTaskFlags.PolymorphicConstruction), GenInLocalFolder]
    public abstract partial class DataNode : IDataNode
    {
        bool dead;
        [GenIgnore] public DataRoot root;
        [GenIgnore] public DataNode carrier;
        
        public StaticConnections staticConnections;
        public int __parent_id;

        public bool IsInHierarchy => !dead;
        public T ReachCarrierHierarchy<T>() where T : DataNode
        {
            var c = carrier;
            while (c != null)
            {
                if (c is T t) return t;
                c = c.carrier;
            }

            return null;
        }

        public void Destroy()
        {
            if (dead)
            {
                Debug.LogError("Destroy called twice");
                return;
            }
            dead = true;
            if (_destroyEvent != null)
            {
                _destroyEvent.Send();
                _destroyEvent.ClearCallbacks();
            }
            OnRemovedFromHierarchy();
        }

        // Calls on any item placed into LivableList or LivableSlot
        // Works only during normal runtime, not during Deserialization or UpdateFrom calls
        public virtual void OnInsertedIntoHierarchy(StaticConnections connections)
        {
            dead = false;
        }

        public void ReInitStaticModifications()
        {
            OnRemovedFromHierarchy();
            OnInsertedIntoHierarchy(staticConnections);
        }
        
        public virtual void OnRemovedFromHierarchy()
        {
            staticConnections?.DisconnectAll(root);
        }

        public IEventStream destroyEvent
        {
            get
            {
                if (_destroyEvent == null) _destroyEvent = new EventStream();
                return _destroyEvent;
            }
        }

        public virtual void ReturnToPool(ObjectPool pool)
        {
        }

//        public virtual void __ForgetIds()
//        {
//        }
        
        [GenIgnore] EventStream _destroyEvent;
    }

    public interface IDataNode
    {
        IEventStream destroyEvent { get; }
    }
}