#if UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
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
            Dictionary<float, Tick> intervalTicks = new Dictionary<float, Tick>();

            public Cell<float> time = new Cell<float>();
            
            class Tick
            {
                public float current;
                public EventStream stream;
            }

            public EventStream TickStream(float delay)
            {
                Tick val;
                if (!intervalTicks.TryGetValue(delay, out val))
                {
                    val = new Tick();
                    val.stream = new EventStream();
                    intervalTicks[delay] = val;
                }
                return val.stream;
            }

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
                time.value += dt;
                for (int i = 0; i < updatables.Count; i++)
                {
                    updatables[i].Update(dt);
                }

                foreach (var tick in intervalTicks)
                {
                    tick.Value.current += dt;
                    if (tick.Value.current > tick.Key)
                    {
                        tick.Value.current -= tick.Key;
                        tick.Value.stream.Send();
                    }
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
            public void Reset()
            {
                value = decay;
            }
            public void Update(float dt)
            {
                value = Mathf.Max(value - dt, 0);
            }
        }
        
        class SpikeCell : Cell<float>, IUpdatable
        {
            public float attackPoint;
            public float platoPoint;
            public float decayPoint;

            float curr = 100000;

            public void Reset()
            {
                curr = 0;
            }
            
            public void Update(float dt)
            {
                curr += dt;
                
                if (curr < attackPoint) 
                    value = curr / attackPoint;
                else if (curr < platoPoint)
                    value = 1;
                else if (curr < decayPoint)
                    value = (decayPoint - curr) / (decayPoint - platoPoint);
                else
                    value = 0;
            }
        }
        
        // On each event it makes |\____|\_______|\_____....
        public static ICell<float> SignalTrigger(this IEventStream e, float decayTime, Action<IDisposable> connectionSink)
        {
            TriggerCell cell = new TriggerCell{decay = decayTime};
            connectionSink(UnityExecuter.instance.AddUpdatable(cell));
            connectionSink(e.Listen(cell.Reset));
            return cell;
        }
        
        // On each event it makes /--\____/--\________....
        public static ICell<float> SignalSpike(this IEventStream e, float attack, float plato, float decay, Action<IDisposable> connectionSink)
        {
            SpikeCell cell = new SpikeCell
            {
                attackPoint = attack,
                platoPoint = attack + plato,
                decayPoint = attack + plato + decay
            };
            connectionSink(UnityExecuter.instance.AddUpdatable(cell));
            connectionSink(e.Listen(cell.Reset));
            return cell;
        }

        // Perlin from -1 to +1
        public static ICell<float> SignalShake(float speed)
        {
            var seed = Random.value * 100000;
            return UnityExecuter.instance.time.Map(val => Mathf.PerlinNoise(val * speed, seed));
        }
        
        // Perlin from -1 to +1
        public static ICell<Vector2> SignalShakeV2(float speed)
        {
            var seed = Random.value * 100000;
            var seed2 = Random.value * 100000;
            return UnityExecuter.instance.time.Map(val => 
                new Vector2(
                    (Mathf.PerlinNoise(val * speed, seed) - 0.5f) * 2, 
                    (Mathf.PerlinNoise(val * speed, seed2) - 0.5f) * 2)
            );
        }
        
        public static IEventStream Interval(float timeInterval)
        {
            return UnityExecuter.instance.TickStream(timeInterval);
        }
    }
}

#endif