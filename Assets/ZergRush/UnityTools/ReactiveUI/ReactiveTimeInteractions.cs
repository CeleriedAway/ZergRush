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

            public void UpdateCustom(float dt)
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

            public void UpdateCustom(float dt)
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
            public float phaseShift = 0;

            public float offset = 0;

            public void Reset()
            {
                time = 0;
            }

            public void Reset(float val)
            {
                time = val;
            }

            public void UpdateCustom(float dt)
            {
                time += dt * speed;
                value = offset + Mathf.Sin(time + phaseShift) * scale;
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

            public void UpdateCustom(float dt)
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

            public void UpdateCustom(float dt)
            {
                remainingTime--;
                if (remainingTime == 0)
                {
                    connection.Dispose();
                    action();
                }
            }
        }

        class DelayedAction : IUpdatable
        {
            private float remainingTime;
            Action action;
            IDisposable connection;

            public DelayedAction(float delay, Action action, bool realtime = false)
            {
                remainingTime = delay;
                this.action = action;
                if (realtime)
                    connection = UnityExecutor.Instance.AddUnscaledUpdatable(this);
                else
                    connection = UnityExecutor.Instance.AddUpdatable(this);
            }

            public void UpdateCustom(float dt)
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

        public static ICell<T> SampleValue<T>(Func<T> sample, IConnectionSink connectionSink)
        {
            var cell = new Cell<T>();
            connectionSink.AddConnection(UnityExecutor.Instance.eachFrame.Subscribe(() => { cell.value = sample(); }));
            return cell;
        }
        
        public static void IfIsTrueForSeconds(ICell<bool> condition, float seconds, Action callback, IConnectionSink sink)
        {
            var time = 0f;
            sink.AddConnection(UnityExecutor.Instance.eachFrame.Subscribe(() =>
            {
                if (condition.value)
                {
                    time += Time.deltaTime;
                    if (time > seconds)
                    {
                        callback();
                    }
                }
                else
                {
                    time = 0;
                }
            }));
        }

        public static IEventStream EventOnceAfterDelay(float delay)
        {
            var e = new EventStream();
            new DelayedAction(delay, () => e.Send());
            return e.Once();
        }

        public static void ExecuteAfterDelay(float delay, Action action)
        {
            new DelayedAction(delay, action);
        }

        public static void ExecuteAfterRealtimeDelay(float delay, Action action)
        {
            new DelayedAction(delay, action, true);
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

            public void UpdateCustom(float dt)
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

            public void UpdateCustom(float dt)
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

        public static CellOfSin SignalSin(this IEventStream reset, float scale, float speed, float resetVal, float phaseShiftRadians,
            IConnectionSink connectionSink)
        {
            CellOfSin cell = new CellOfSin {scale = scale, speed = speed, phaseShift = phaseShiftRadians};
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

            public void UpdateCustom(float dt)
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

}
