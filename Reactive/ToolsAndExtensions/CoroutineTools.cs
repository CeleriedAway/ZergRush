#if UNITY_5_3_OR_NEWER

using System;
using System.Collections;
using ZergRush.ReactiveCore;
using UnityEngine;

namespace ZergRush
{
    public class WaitResult<T>
    {
        public T value;
    }

    public class WaitForEvent : CustomYieldInstruction
    {
        IDisposable connection;
        bool ready;
        float timeout;
        public WaitForEvent(IEventStream stream, float timeout = -1)
        {
            this.timeout = timeout;
            connection = stream.Subscribe(() =>
            {
                connection.DisconnectSafe();
                ready = true;
                connection = null;
            });
            if (ready) connection.Dispose();
        }

        public override bool keepWaiting
        {
            get
            {
                if (timeout > 0)
                {
                    timeout -= Time.deltaTime;
                    if (timeout <= 0) return false;
                }
                return ready == false;
            }
        }
    }

    public class WaitForEvent<T> : CustomYieldInstruction
    {
        IDisposable connection;
        bool ready;
        public WaitForEvent(IEventStream<T> eventStream, WaitResult<T> result)
        {
            connection = eventStream.Subscribe(t =>
            {
                ready = true;
                result.value = t;
                connection.DisconnectSafe();
                connection = null;
            });
            if (ready) connection.Dispose();
        }
        public override bool keepWaiting { get { return ready == false; } }
    }

    public class DoForSomeTime : IEnumerator
    {
        public float time;
        public Action action;

        public DoForSomeTime(float time, Action action)
        {
            this.time = time;
            this.action = action;
        }

        public bool MoveNext()
        {
            action();
            time -= Time.deltaTime;
            return time > 0;
        }

        public void Reset() {}

        public object Current => null;
    }
}

#endif