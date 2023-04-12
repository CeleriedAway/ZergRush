using System;
using System.IO;
using Newtonsoft.Json;
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
    public abstract partial class DataNode : IDataNode, IReferencableFromDataRoot
    {
        bool dead;
        [GenIgnore] public DataRoot root;
        [GenIgnore] public DataNode carrier;
        
        public StaticConnections staticConnections;
        //public int __parent_id;

        public bool IsInHierarchy => !dead;
        
        // for debug
        [GenIgnore] protected DataNode previousCarrier;
        
        public void SetRootAndCarrier(DataRoot root, DataNode carrier)
        {
            this.root = root;
            previousCarrier = carrier;
            this.carrier = carrier;
        }
        
        public T ReachCarrierHierarchy<T>() where T : DataNode
        {
            var c = this;
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
                if (LogSink.errLog != null) LogSink.errLog("Destroy called twice");
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
        
        public virtual void VisitNode(Action<object> visitor)
        {
            visitor(this);
        }

        [GenIgnore] EventStream _destroyEvent;
        
        // Use HasRefId class tag for codegen to generate Id logic
        // must be here unfortunately because console gen need interfaces to be implemented
        public virtual int Id
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public virtual bool supportId => false;
    }

    public interface IDataNode 
    {
        IEventStream destroyEvent { get; }
        bool IsInHierarchy { get; }
    }
}