using System;
using Newtonsoft.Json;
using UnityEngine;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    [GenInLocalFolder, GenTask(GenTaskFlags.CompareChech), GenTaskCustomImpl(GenTaskFlags.PooledUpdateFrom | GenTaskFlags.JsonSerialization | GenTaskFlags.DefaultConstructor)]
    public sealed partial class Ref<T> : DataNode, ICell<T>, IConnectable
        where T : class, IReferencableFromDataRoot
    {
        int __id;
        int _id
        {
            get => __id;
            set
            {
                // if (__id != value)
                // {
                //     Debug.Log($"id set from {__id} to {value}");
                // }
                __id = value;
            }
        }
        [GenIgnore] T cachedVal;
        
        [GenIgnore] private EventStream<T> up;
        public IDisposable ListenUpdates(Action<T> reaction)
        {
            if (up == null) up = new EventStream<T>();
            return up.Subscribe(reaction);
        }

        public int id
        {
            get
            {
                if (_id == 0 && cachedVal != null)
                {
                    throw new NotImplementedException();
                }
                return _id;
            }
            set 
            { 
                _id = value;
                cachedVal = null;
            }
        }

        public T value
        {
            get
            {
                //Debug.Log($"id: {_id} cached {cachedVal}");
                if (cachedVal == null)
                {
                    if (_id == 0)
                    {
                        //Debug.Log($"id == 0 returning null");
                        return null;
                    }
                    var recolled = root.RecallMayBe(_id);
                    if (recolled == null)
                    {
                        //_id = 0;
                        
                        //Debug.Log($"recolled is null returning null");
                        return null;
                    }
                    cachedVal = recolled as T;
                    if (cachedVal == null)
                    {
                        throw new ZergRushException("invalid object stored with id: " + _id);
                    }
                }
                else if (_id != cachedVal.Id)
                {
                    cachedVal = null;
                }

                //Debug.Log($"returning cached {cachedVal}");
                return cachedVal;
            }
            set
            {
                if (value == null)
                {
                    _id = 0;
                    cachedVal = null;
                }
                else
                {
                    cachedVal = value;
                    if (value.Id == 0)
                    {
                        throw new ZergRushException($"value {value} set with 0 id");
                    }

                    _id = value.Id;
                }

                if (up != null) { up.Send(value); }
            }
        }

        public override void WriteJsonFields(JsonTextWriter writer)
        {
            base.WriteJsonFields(writer);
        }

        public override void ReadFromJsonField(JsonTextReader reader, string name)
        {
            base.ReadFromJsonField(reader, name);
        }

        public static implicit operator T(Ref<T> r)
        {
            return r.value;
        }

        public int getConnectionCount => up == null ? 0 : up.getConnectionCount;
    }
}

