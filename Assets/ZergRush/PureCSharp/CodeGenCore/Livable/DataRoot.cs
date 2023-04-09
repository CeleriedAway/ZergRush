//#define LogRegistering

using System;
using System.Collections.Generic;
using UnityEngine;
using ZergRush.Alive;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    [GenInLocalFolder]
    public abstract partial class DataRoot : DataNode
    {
        [GenIgnore] Dictionary<int, object> gameEntities = new Dictionary<int, object>();
        [GenIgnore] public ObjectPool pool;
        
        // Ignore almost all id interaction during updatefrom 
        [GenIgnore] public bool __updating;
        [GenIgnore] public string __debugTag;

        [GenIgnore] List<INeedUpdateFromPostProcess> updatePostProcess = new List<INeedUpdateFromPostProcess>();

        public void __RegisterUpdatePostprocess(INeedUpdateFromPostProcess p)
        {
            updatePostProcess.Add(p);
        }


        public int nexId => entityIdFactory++;
        public int __entityIdFactory = 1;
        public int entityIdFactory
        {
            get { return __entityIdFactory; }
            set
            {
                __entityIdFactory = value;
            }
        }

        // you should call this only when otherData is loaded from somewhere, its id structure will be trashed in process
        public virtual void RootUpdateFromPartial<T>(T selfData, T otherData) where T : DataNode
        {
            otherData.root = this;
            Dictionary<int, int> idDict = new Dictionary<int, int>();
            otherData.VisitNode(node =>
            {
                if (node is IReferencableFromDataRoot idNode && idNode.supportId)
                {
                    var currId = idNode.Id;
                    var newId = __entityIdFactory++;
                    idDict[currId] = newId;
                    idNode.Id = newId;
                    LogSink.log($"{node} id {currId} -> {newId}");
                }
            });
            // otherData.__GenIds(this);
            // otherData.VisitNode(node => node.OnInsertedIntoHierarchy(node.staticConnections));
            __updating = true;
            selfData.UpdateFrom(otherData);
            __updating = false;
            foreach (var needUpdateFromPostProcess in updatePostProcess)
            {
                needUpdateFromPostProcess.OnUpdateFinished();
            }
            updatePostProcess.Clear();
            
            selfData.VisitNode(obj =>
            {
                var node = obj as DataNode;
                if (node == null) return;
                //LogSink.log($"updating static connections for {node}");
                for (var i = 0; i < node.staticConnections.connections.Count; i++)
                {
                    var conn = node.staticConnections.connections[i];
                    if (idDict.ContainsKey(conn.ownerId) == false || idDict.ContainsKey(conn.entityId) == false)
                    {
                        LogSink.errLog($"idDict doesn't contain {conn.ownerId} or {conn.entityId}");
                        continue;
                    }
                    conn.ownerId = idDict[conn.ownerId];
                    conn.entityId = idDict[conn.entityId];
                    node.staticConnections.connections[i] = conn;
                }
            });
        }
        public virtual void RootUpdateFrom(DataRoot other, ZRUpdateFromHelper __helper)
        {
            // All ids will be refilled from other model
            gameEntities.Clear();
            __updating = true;
            
            UpdateFrom(other, __helper);
            this.__entityIdFactory = other.__entityIdFactory;
            
            __updating = false;
            
            foreach (var needUpdateFromPostProcess in updatePostProcess)
            {
                needUpdateFromPostProcess.OnUpdateFinished();
            }
            updatePostProcess.Clear();
        }
        
        public void Remember(object entity, int id)
        {
            if (id == 0)
            {
                throw new ZergRushException($"zero id for entity {entity}");
            }
            //if (updating) return;
            if (gameEntities.TryGetValue(id, out var val))
            {
                if (ReferenceEquals(val, entity))
                {
                    return;
                }
                #if LogRegistering
                Debug.LogError($"This id {id} of {entity} is already taken by entity: {gameEntities[id]} id={id}");
                #endif
                return;
            }
            #if LogRegistering
            Debug.Log($"RegisterEntity {entity.ToString()} id={id}");
            #endif
            gameEntities.Add(id, entity);
        }

        public bool HasEntityWithId(int id)
        {
            return gameEntities.ContainsKey(id);
        }
        public object Recall(int id)
        {
            #if LogRegistering
            if (gameEntities.ContainsKey(id) == false)
            {
                Debug.LogError($"entity with id: {id} was not found, but it is fine (not really)");
                return null;
            }
            #endif
            return gameEntities[id];
        }
        
        public T Recall<T>(int id) where T : class
        {
            return Recall(id) as T;
        }
        
        public T RecallMayBe<T>(int id) where T : class
        {
            return RecallMayBe(id) as T;
        }
        
        public object RecallMayBe(int id)
        {
            if (id == 0) return null;
            object val;
            if (gameEntities.TryGetValue(id, out val))
            {
                if (id != ((IReferencableFromDataRoot) val).Id)
                {
                    LogSink.errLog?.Invoke("fuck up");
                }
                return val;
            }
            return null;
        }

        public void Forget(int id, object entity)
        {
            if (__updating) return;
            
            #if LogRegistering
            Debug.Log($"DeregisterEntity {entity.ToString()} id={id}");
            #endif
            object storedEntity;
            if (gameEntities.TryGetValue(id, out storedEntity))
            {
                if (object.ReferenceEquals(storedEntity, entity))
                {
                    #if LogRegistering
                    Debug.Log($"removing {id} {entity}");
                    #endif
                    gameEntities.Remove(id);
                }
                else
                {
                    #if LogRegistering
                    Debug.Log("different entity was stored with same id");
                    #endif
                }
            }
            else
            {
                #if LogRegistering
                Debug.Log("no entity was stored with this id");
                #endif
            }
        }


        public void ForceId(int newId, object obj)
        {
            //if (!updating) Debug.LogError($"This one should be called only during update from {obj} {newId}");
            __entityIdFactory = Math.Max(__entityIdFactory, newId + 1);
            gameEntities[newId] = obj;
        }

        public void ChangeEntityId(int oldId, int newId, DataNode entity)
        {
            #if LogRegistering
            Debug.Log($"ChangeEntityId {entity.ToString()} prev id={oldId}, new id={newId}");
            #endif
            object prevVal;
            if (oldId > 0 && gameEntities.TryGetValue(oldId, out prevVal))
            {
                if (object.ReferenceEquals(prevVal, entity))
                {
                    gameEntities.Remove(oldId);
                }
                else
                {
                    #if LogRegistering
                    Debug.Log($"different object was stored for old id, old entity = {prevVal.ToString()}");
                    #endif
                }
            }

            gameEntities[newId] = entity;
        }
    }
}