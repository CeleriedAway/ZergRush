//#define LogRegistering
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
        [GenIgnore] bool updating => updatingProcess.value;
        [GenIgnore] public string __debugTag;

        [GenIgnore] public Cell<bool> updatingProcess = new Cell<bool>();

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

        public virtual void RootUpdateFrom(DataRoot other)
        {
            // All ids will be refilled from other model
            
            gameEntities.Clear();
            updatingProcess.value = true;
            
            UpdateFrom(other);
            this.__entityIdFactory = other.__entityIdFactory;
            
            updatingProcess.value = false;
            
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
                Debug.LogError($"This id {id} of {entity} is already taken by entity: {gameEntities[id]} id={id}");
                return;
            }
            #if LogRegistering
            Debug.Log($"RegisterEntity {entity.ToString()} id={id}");
            #endif
            gameEntities.Add(id, entity);
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
            object val;
            if (gameEntities.TryGetValue(id, out val))
            {
                if (id != ((IReferencableFromDataRoot) val).Id)
                {
                    Debug.LogError("fuck up");
                }
                return val;
            }
            return null;
        }

        public void Forget(int id, object entity)
        {
            if (updating) return;
            
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