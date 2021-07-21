#if UNITY_5_3_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using ZergRush.ReactiveCore;

namespace ZergRush
{
    public static partial class ReactiveTimeInteractions
    {
        class AnonymousUpdatable : IUpdatable
        {
            Action<float> update;

            public AnonymousUpdatable(Action<float> update)
            {
                this.update = update;
            }

            public void Update(float dt)
            {
                update?.Invoke(dt);
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

        // Satanic programming.
        public class CellOfSin : Cell<float>, IUpdatable
        {
            public float scale;
            public float time;
            public float speed = 1;

            public float offset = 0;

            public void Reset()
            {
                time = 0;
            }

            public void Reset(float val)
            {
                time = val;
            }

            public void Update(float dt)
            {
                time += dt * speed;
                value = offset + Mathf.Abs(Mathf.Sin(time)) * scale;
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

        public static IEventStream EachFrameEvent()
        {
            return UnityExecutor.Instance.eachFrame;
        }

        public static void ExecuteEachFrame(Action action, IConnectionSink connectionSink)
        {
            connectionSink.AddConnection(UnityExecutor.Instance.eachFrame.Subscribe(action));
        }

        class FrameDelayedAction : IUpdatable
        {
            private int remainingTime;
            Action action;
            IDisposable connection;

            public FrameDelayedAction(int delay, Action action)
            {
                remainingTime = delay;
                this.action = action;
                connection = UnityExecutor.Instance.AddUpdatable(this);
            }

            public void Update(float dt)
            {
                remainingTime--;
                if (remainingTime == 0)
                {
                    connection.Dispose();
                    action();
                }
            }
        }

        class WaitingDelayedAction : IUpdatable
        {
            private float remainingTime;
            Action action;
            IDisposable connection;

            public WaitingDelayedAction(float delay, Action action, bool realtime = false)
            {
                remainingTime = delay;
                this.action = action;
                if (realtime)
                    connection = UnityExecutor.Instance.AddUnscaledUpdatable(this);
                else
                    connection = UnityExecutor.Instance.AddUpdatable(this);
            }

            public void Update(float dt)
            {
                remainingTime -= dt;
                if (remainingTime < 0)
                {
                    connection.Dispose();
                    action();
                }
            }
        }

        public static void ExecuteAfterCondition(Func<bool> condition, Action action)
        {
            if (condition())
                action();
            else
            {
                IDisposable updating = null;
                updating = UnityExecutor.Instance.eachFrame.Subscribe(() =>
                {
                    if (!condition())
                        return;
                    updating.Dispose();
                    action();
                });
            }
        }

        public static IEventStream EventOnceAfterDelay(float delay)
        {
            var e = new EventStream();
            new WaitingDelayedAction(delay, () => e.Send());
            return e.Once();
        }

        public static void ExecuteAfterDelay(float delay, Action action)
        {
            new WaitingDelayedAction(delay, action);
        }

        public static void ExecuteAfterRealtimeDelay(float delay, Action action)
        {
            new WaitingDelayedAction(delay, action, true);
        }

        public static void ExecuteNextUpdate(Action action)
        {
            new FrameDelayedAction(1, action);
        }

        public static void ExecuteAfterFrames(int frames, Action action)
        {
            new FrameDelayedAction(frames, action);
        }

        class Resampler : ICell<float>, IUpdatable
        {
            public ICell<float> source;
            EventStream<float> update;

            public void Update(float dt)
            {
                update.Send(source.value);
            }

            public IDisposable ListenUpdates(Action<float> reaction)
            {
                return update.Subscribe(reaction);
            }

            public float value => source.value;
        }

        // 
        public static ICell<float> Resample(this ICell<float> signal, IConnectionSink connectionSink)
        {
            var resampler = new Resampler {source = signal};
            connectionSink.AddConnection(UnityExecutor.Instance.AddUpdatable(resampler));
            return resampler;
        }


        public class LaggedCell<T> : Cell<T>, IUpdatable
        {
            public LaggedCell(ICell<T> source, float lag = 1, Predicate<T> isValueLagged = null,
                IConnectionSink connectionSink = null)
            {
                value = source.value;
                if (connectionSink == null) connectionSink = new Connections();
                connectionSink.AddConnection(UnityExecutor.Instance.AddUpdatable(this));
                connectionSink.AddConnection(source.ListenUpdates(val =>
                {
                    if (isValueLagged == null || isValueLagged(val))
                    {
                        laggedValue = val;
                        lagRemaining = lag;
                    }
                    else
                    {
                        value = val;
                        lagRemaining = -1; // Kill currently lagged value, it will never be shown.
                    }
                }));
            }

            T laggedValue;
            float lagRemaining = -1;

            public void Update(float dt)
            {
                if (lagRemaining <= 0) return;
                lagRemaining -= dt;
                if (lagRemaining <= 0)
                    value = laggedValue;
            }
        }

        public static ICell<T> Lag<T>(this ICell<T> e, float lag = 1, Predicate<T> isValueLagged = null,
            IConnectionSink connectionSink = null)
            => new LaggedCell<T>(e, lag, isValueLagged, connectionSink);

        // On each event it makes |\____|\_______|\_____....
        public static ICell<float> SignalTrigger(this IEventStream e, float decayTime, IConnectionSink connectionSink)
        {
            TriggerCell cell = new TriggerCell {decay = decayTime};
            connectionSink.AddConnection(UnityExecutor.Instance.AddUpdatable(cell));
            connectionSink.AddConnection(e.Subscribe(cell.Reset));
            return cell;
        }

        // On each event it makes /--\____/--\________....
        public static ICell<float> SignalSpike(this IEventStream e, float attack, float plato, float decay,
            IConnectionSink connectionSink)
        {
            SpikeCell cell = new SpikeCell
            {
                attackPoint = attack,
                platoPoint = attack + plato,
                decayPoint = attack + plato + decay
            };
            connectionSink.AddConnection(UnityExecutor.Instance.AddUpdatable(cell));
            connectionSink.AddConnection(e.Subscribe(cell.Reset));
            return cell;
        }

        public static CellOfSin SignalSin(this IEventStream reset, float scale, float speed, float resetVal,
            IConnectionSink connectionSink)
        {
            CellOfSin cell = new CellOfSin {scale = scale, speed = speed};
            connectionSink.AddConnection(UnityExecutor.Instance.AddUpdatable(cell));
            if (reset != null) connectionSink.AddConnection(reset.Subscribe(() => cell.Reset(resetVal)));
            return cell;
        }

        // Perlin from -1 to +1
        public static ICell<float> SignalShake(float speed)
        {
            var seed = Random.value * 100000;
            return UnityExecutor.Instance.time.Map(val => Mathf.PerlinNoise(val * speed, seed));
        }

        // Perlin from -1 to +1
        public static ICell<Vector2> SignalShakeV2(float speed)
        {
            var seed = Random.value * 100000;
            var seed2 = Random.value * 100000;
            return UnityExecutor.Instance.time.Map(val =>
                new Vector2(
                    (Mathf.PerlinNoise(val * speed, seed) - 0.5f) * 2,
                    (Mathf.PerlinNoise(val * speed, seed2) - 0.5f) * 2)
            );
        }

        public static IEventStream<float> TimeAccumulator(this ICell<float> val, float interval,
            IConnectionSink connectionSink)
        {
            var accum = 0f;
            connectionSink.AddConnection(val.Bind(v => accum += v));
            return UnityExecutor.Instance.TickStream(interval).Map(() =>
            {
                var currAcc = accum;
                accum = 0;
                return currAcc;
            });
        }

        public static IEventStream GlobalInterval(float timeInterval)
        {
            return UnityExecutor.Instance.TickStream(timeInterval);
        }

        public class TickUpdatable : EventStream, IUpdatable
        {
            float interval;
            float cur;

            public TickUpdatable(float interval)
            {
                this.interval = interval;
            }

            public void Update(float dt)
            {
                cur += dt;
                if (cur > interval)
                {
                    cur = 0;
                    Send();
                }
            }
        }

        public static IEventStream Interval(float timeInterval, IConnectionSink connectionSink)
        {
            var e = new TickUpdatable(timeInterval);
            connectionSink.AddConnection(UnityExecutor.Instance.AddUpdatable(e));
            return e;
        }

        public static IEventStream Interval30FPS()
        {
            return UnityExecutor.Instance.TickStream(1 / 30f);
        }

        public static IEventStream Interval10FPS()
        {
            return UnityExecutor.Instance.TickStream(1 / 10f);
        }

        public static ICell<float> DecayingPush(this IEventStream<float> e, float ampPerPush, float asymptote,
            IConnectionSink sink)
        {
            Cell<float> value = new Cell<float>();
            sink.AddConnection(e.Subscribe(val =>
            {
                value.value = Mathf.Atan((value.value + ampPerPush * val) / asymptote) / (Mathf.PI / 4) * asymptote;
            }));
            sink.AddConnection(
                UnityExecutor.Instance.AddUpdatable(new AnonymousUpdatable(dt => { value.value /= 1.1f; })));
            return value;
        }

        // not thread safe
        internal static class GaussFilterWeightsCache
        {
            static Dictionary<int, float[]> cache = new Dictionary<int, float[]>();

            public static float [] GetWeights(int samples)
            {
                if (cache.TryGetValue(samples, out var cachedWeights))
                {
                    return cachedWeights;
                }

                var weights = new float[samples];
                float weightAccum = 0;
                for (int i = 0; i < samples; i++)
                {
                    float sigma = samples * samples * 2;
                    var newWeight = Mathf.Exp(-i * i / sigma);
                    weights[i] = newWeight;
                    weightAccum += newWeight;
                }

                for (int i = 0; i < samples; i++)
                {
                    weights[i] /= weightAccum;
                }

                cache[samples] = weights;
                return weights;
            }
        }

        public class GaussFilterBufferBase<T> : Cell<T>
        {
            protected CycleBuffer<T> valueBuffer;

            public GaussFilterBufferBase(int samples)
            {
                valueBuffer = new CycleBuffer<T>(samples);
            }

            public GaussFilterBufferBase(int samples, T prefillValue)
            {
                valueBuffer = new CycleBuffer<T>(samples, prefillValue);
            }
            
            public void Fill(T value)
            {
                valueBuffer.Fill(value);
            }
        }

        public static ICell<float> GaussFilter(this ICell<float> value, IEventStream sampler, int samples,
            IConnectionSink connectionSink, bool prefill = false)
        {
            var filter = prefill ? new GaussFilteredFloat(samples, value.value) : new GaussFilteredFloat(samples);
            connectionSink.AddConnection(sampler.Subscribe(() => { filter.PushValue(value.value); }));
            return filter;
        }

        public static ICell<Vector3> GaussFilter(this ICell<Vector3> value,
            IEventStream sampler, int samples, IConnectionSink connectionSink, bool prefill = false)
        {
            var filter = prefill ? new GaussFilteredVector(samples, value.value) : new GaussFilteredVector(samples);
            connectionSink.AddConnection(sampler.Subscribe(() => { filter.PushValue(value.value); }));
            return filter;
        }

        //TODO- Make valid in the first tick
        public static ICell<float> Derivative(this ICell<float> value, float sampleInterval,
            IConnectionSink connectionSink)
        {
            Cell<float> derivative = new Cell<float>();
            float lastValue = value.value;
            connectionSink.AddConnection(UnityExecutor.Instance.TickStream(sampleInterval).Subscribe(() =>
            {
                var newVal = value.value;
                derivative.value = (newVal - lastValue) / sampleInterval;
                lastValue = newVal;
            }));
            return derivative;
        }

        public static ICell<float> MovementSpeed(this ICell<Vector3> value, float sampleInterval,
            IConnectionSink connectionSink)
        {
            Cell<float> derivative = new Cell<float>();
            var lastValue = value.value;
            connectionSink.AddConnection(UnityExecutor.Instance.TickStream(sampleInterval).Subscribe(() =>
            {
                var newVal = value.value;
                derivative.value = Vector3.Distance(newVal, lastValue) / sampleInterval;
                lastValue = newVal;
            }));
            return derivative;
        }

        public static IDisposable StartCoroutine(IEnumerator coro)
        {
            var coroHandle = UnityExecutor.Instance.StartCoroutine(coro);
            return new AnonymousDisposable(() => UnityExecutor.Instance.StopCoroutine(coroHandle));
        }
    }

    public class CycleBuffer<T>
    {
        int currentIndex = -1;
        int total;
        int max;
        T[] values;


        public CycleBuffer(int sampleCount)
        {
            max = sampleCount;
            values = new T[sampleCount];
        }

        public CycleBuffer(int sampleCount, T prefillValue) : this(sampleCount)
        {
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = prefillValue;
            }
        }
        
        public CycleBuffer(int sampleCount, Func<T> prefillFactory) : this(sampleCount)
        {
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = prefillFactory();
            }
        }

        public int Count => total < max ? total : max;

        // index sampling goes from newest value to latest at the end
        public T Sample(int index)
        {
            return values[(currentIndex - index + max) % max];
        }

        public bool Filled => total == max;

        // index == 0 is the last one, 1 is the previous and so on
        public void ForEach(Action<int, T> action)
        {
            //Debug.Log(values.PrintCollection());
            if (currentIndex < 0) return;
            int limit = Count;
            for (int i = 0; i < limit; i++)
            {
                action(i, Sample(i));
            }
        }

        public void Fill(T value)
        {
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = value;
            }
        }

        public void Push(T value)
        {
            currentIndex = (currentIndex + 1) % max;
            if (total != max) total = currentIndex + 1;
            values[currentIndex] = value;
        }
            
        public T PushAndReturnPrev(T value)
        {
            currentIndex = (currentIndex + 1) % max;
            if (total != max) total = currentIndex + 1;
            var prev = values[currentIndex];
            values[currentIndex] = value;
            return prev;
        }

        // Pushes same value that was in buffer before
        public T PushCached()
        {
            currentIndex = (currentIndex + 1) % max;
            if (total != max) total = currentIndex + 1;
            return values[currentIndex];
        }
    }

    public class GaussFilteredVector : ReactiveTimeInteractions.GaussFilterBufferBase<Vector3>
    {
        public Vector3 PushValue(Vector3 value)
        {
            valueBuffer.Push(value);
            var c = valueBuffer.Count;
            var weights = ReactiveTimeInteractions.GaussFilterWeightsCache.GetWeights(c);
            Vector3 result = Vector3.zero;
            for (int i = 0; i < c; i++)
            {
                var gauss = weights[i];
                result += gauss * valueBuffer.Sample(i);
            }
            this.value = result;
            return result;
        }

        public GaussFilteredVector(int samples) : base(samples)
        {
        }

        public GaussFilteredVector(int samples, Vector3 prefillValue) : base(samples, prefillValue)
        {
        }
    }

    public class GaussFilteredFloat : ReactiveTimeInteractions.GaussFilterBufferBase<float>
    {
        public float PushValue(float value)
        {
            valueBuffer.Push(value);
            var c = valueBuffer.Count;
            var weights = ReactiveTimeInteractions.GaussFilterWeightsCache.GetWeights(c);
            float result = 0;
            for (int i = 0; i < c; i++)
            {
                var gauss = weights[i];
                result += gauss * valueBuffer.Sample(i);
            }
            this.value = result;
            return value;
        }

        public GaussFilteredFloat(int samples) : base(samples)
        {
        }

        public GaussFilteredFloat(int samples, float prefillValue) : base(samples, prefillValue)
        {
        }
    }
}

#endif