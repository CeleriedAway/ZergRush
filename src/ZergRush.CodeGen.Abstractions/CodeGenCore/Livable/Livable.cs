using System;
using System.Collections;
using System.Collections.Generic;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    /*
     *     Livable object makes event/cells connection and other influences on data model in EnliveSelf method
     *     All those connections would be automatically disposed when object is mortified
     *     Enlive and Mortify will be automatically called when added or removed to special containers like LivableSlot/LivableList
     *     So you never call Enlive methods manually
     */
    [GenTask(GenTaskFlags.LivableNodePack & ~GenTaskFlags.PolymorphicConstruction), GenZergRushFolder()]
    public abstract partial class Livable : ILivableNode, IConnectionSink, ILivable
    {
        bool dead;
        [GenIgnore] public LivableRoot root;
        [GenIgnore] public Livable carrier;
        
        public bool IsInHierarchy => !dead;
        
        // for debug
        [GenIgnore] protected Livable previousCarrier;

        [GenIgnore] public bool isAlive { get; private set; }
        [GenIgnore] List<Connection> fastConnections;
        [GenIgnore] List<Action> normalConnections;

        public void SetRootAndCarrier(LivableRoot root, Livable carrier)
        {
            this.root = root;
            previousCarrier = carrier;
            this.carrier = carrier;
        }
        
        public T ReachCarrierHierarchy<T>() where T : Livable
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

        // Calls on any item placed into LivableList or LivableSlot.
        public virtual void OnInsertedIntoHierarchy()
        {
            dead = false;
        }

        public void ReInitStaticModifications()
        {
            OnRemovedFromHierarchy();
            OnInsertedIntoHierarchy();
        }
        
        public virtual void OnRemovedFromHierarchy()
        {
        }

        public IEventStream destroyEvent
        {
            get
            {
                if (_destroyEvent == null) _destroyEvent = new EventStream();
                return _destroyEvent;
            }
        }

        public virtual void VisitNode(Action<object> visitor)
        {
            visitor(this);
        }

        public virtual void UpdateFrom(Livable other)
        {
            UpdateFrom(other, new ZRUpdateFromHelper());
        }

        public virtual void UpdateFrom(Livable other, ZRUpdateFromHelper __helper)
        {
        }

        public virtual void __PropagateHierarchy()
        {
        }

        public virtual void Enlive()
        {
            EnliveSelf();
            EnliveChildren();
        }

        public virtual void Mortify()
        {
            MortifySelf();
            MortifyChildren();
        }

        protected virtual void EnliveChildren()
        {
        }

        protected virtual void MortifyChildren()
        {
        }

        [GenIgnore] EventStream _destroyEvent;

        public void DisconnectAll()
        {
            if (fastConnections != null)
            {
                for (var i = 0; i < fastConnections.Count; i++)
                {
                    var connection = fastConnections[i];
                    connection.Disconnect();
                }
                fastConnections.Clear();
            }
            if (normalConnections != null)
            {
                for (var i = 0; i < normalConnections.Count; i++)
                {
                    var inf = normalConnections[i];
                    inf();
                }
                normalConnections.Clear();
            }
        }

        public void AddConnection(Connection conn)
        {
            if (fastConnections == null) fastConnections = new List<Connection>();
            fastConnections.Add(conn);
        }

        public void AddInfluence(Action effect)
        {
            if (normalConnections == null) normalConnections = new List<Action>();
            normalConnections.Add(effect);
        }
        public void AddInfluence(IDisposable effect)
        {
            if (normalConnections == null) normalConnections = new List<Action>();
            normalConnections.Add(effect.Dispose);
        }
        public IDisposable addConnection
        {
            set => AddInfluence(value);
        }

        public bool HasConnections()
        {
            return (normalConnections != null && normalConnections.Count > 0) || (fastConnections != null && fastConnections.Count > 0);
        }

        public void DisconnectConcreteInfluence(Action effect)
        {
            if (normalConnections == null) return;

            if (!normalConnections.Contains(effect))
            {
                throw new ZergRushException("You can not disconnect influence because it does not exist");
            }

            effect();

            normalConnections.Remove(effect);
        }
        
        protected virtual void EnliveSelf()
        {
            if (isAlive)
            {
                throw new ZergRushException($"You can not enlive living, may be you place same instance of this class" +
                                $" {this} several times into LivableList or into LivableSlot or as readonly member of other livable class," +
                                $" previous carrier {previousCarrier} current carrier {carrier}");
            }

            isAlive = true;
        }

        protected virtual void MortifySelf()
        {
            if (!isAlive)
            {
                throw new ZergRushException("What Is Dead May Never Die (c) or probably an internal ZR error");
            }

            DisconnectAll();
            isAlive = false;
        }


        public void AddConnection(IDisposable connection)
        {
            AddInfluence(connection.Dispose);
        }
    }
    
    public struct Connection
    {
        public IList reader;
        public object obj;

        public void Disconnect()
        {
            if (reader != null)
            {
                reader.Remove(obj);
                reader = null;
                obj = null;
            }
        }
    }

    public interface ILivableNode
    {
        bool IsInHierarchy { get; }
        IEventStream destroyEvent { get; }
    }
}
