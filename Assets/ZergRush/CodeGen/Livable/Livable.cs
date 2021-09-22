using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZergRush.CodeGen;

namespace ZergRush.Alive
{
    /*
     *     Livable object makes event/cells connection and other influences on data model in EnliveSelf method
     *     All those connections would be automatically disposed when object is mortified
     *     Enlive and Mortify will be automatically called when added or removed to special containers like LivableSlot/LivableList
     *     So you never call Enlive methods manually
     */
    [GenTask(GenTaskFlags.LivableNodePack & ~GenTaskFlags.PolymorphicConstruction), GenInLocalFolder]
    public abstract partial class Livable : DataNode, IConnectionSink
    {
        [GenIgnore] public bool alive { get; private set; }
        [GenIgnore] List<Connection> fastConnections;
        [GenIgnore] List<Action> normalConnections;

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
            if (alive)
            {
                throw new ZergRushException("You can not enlive living");
            }

            alive = true;
        }

        protected virtual void MortifySelf()
        {
            if (!alive)
            {
                throw new ZergRushException("What Is Dead May Never Die (c)");
            }

            DisconnectAll();
            alive = false;
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

    public interface IStaticallyModifiable
    {
        void DisposeAffect(int itemId);
    }
    
    [GenInLocalFolder]
    public class StaticConnections
    {
        public int ownerId;
        public List<SerializableConnection> connections = new List<SerializableConnection>();

        public void DisconnectAll(DataRoot root)
        {
            foreach (var connection in connections)
            {
                var obj = root.RecallMayBe<IStaticallyModifiable>(connection.ownerId);
                if (obj == null)
                {
                    Debug.LogError($"can't find IStaticallyModifiable obj with id {connection.ownerId} simple recall: {root.RecallMayBe(connection.ownerId)}");
                    continue;
                }
                obj.DisposeAffect(connection.entityId);
            }
            connections.Clear();
        }

        public void Add(SerializableConnection conn)
        {
            if (conn.entityId == 0)
            {
                Debug.LogError("static connection entity id is 0");
            }
            if (conn.ownerId == 0)
            {
                Debug.LogError("static connection owner id is 0");
            }
            connections.Add(conn);
        }
    }
    
    [GenInLocalFolder]
    public struct SerializableConnection
    {
        public SerializableConnection(int ownerId, int entityId)
        {
            this.ownerId = ownerId;
            this.entityId = entityId;
        }

        public int ownerId;
        public int entityId;
    }
}
