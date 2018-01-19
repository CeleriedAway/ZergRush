#if UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEngine;
using ZergRush.ReactiveCore;

namespace ZergRush
{
    public static class ReactiveTimeInteractions
    {
        interface IUpdatable
        {
            void Update(float dt);
        }
        class UnityExecuter : MonoBehaviour
        {
            static UnityExecuter instance_val;
            
            List<IUpdatable> updatables = new List<IUpdatable>();

            public void RegisterUpdatable(IUpdatable updatable)
            {
                updatables.Add(updatable);
            }
            public void RemoveUpdatable(IUpdatable updatable)
            {
                updatables.Remove(updatable);
            }

            class RemoveUpdateDisposable : IDisposable
            {
                public IUpdatable updatable;
                public void Dispose()
                {
                    UnityExecuter.instance.RemoveUpdatable(updatable);
                }
            }
            public IDisposable AddUpdatable(IUpdatable updatable)
            {
                RegisterUpdatable(updatable);
                return new RemoveUpdateDisposable{updatable = updatable};
            }

            void Update()
            {
                float dt = Time.deltaTime;
                for (int i = 0; i < updatables.Count; i++)
                {
                    updatables[i].Update(dt);
                }
            }

            public static UnityExecuter instance
            {
                get
                {
                    if (instance_val == null)
                    {
                        var obj = new GameObject("ZergRushExecuter");
                        instance_val = obj.AddComponent<UnityExecuter>();
                        GameObject.DontDestroyOnLoad(instance_val);
                    }
                    return instance_val;
                }
            }
        }

        class TriggerCell : Cell<float>, IUpdatable
        {
            public float decay;
            public void Update(float dt)
            {
                value = value *= Mathf.Pow(decay, dt);
            }
        }
        
        // TODO finish this.
        public static ICell<float> SignalTrigger(this IEventStream e, float decayTime, Action<IDisposable> connectionSink)
        {
            TriggerCell cell = new TriggerCell{decay = decayTime};
            connectionSink(UnityExecuter.instance.AddUpdatable(cell));
            connectionSink(e.Listen(() => cell.value = 1));
            return cell;
        }
    }
}

#endif