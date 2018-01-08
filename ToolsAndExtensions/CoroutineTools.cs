#if UNITY_5_3_OR_NEWER

using System;
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
            connection = stream.Listen(() =>
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
        private IDisposable connection;
        bool ready;
        public WaitForEvent(IEventStream<T> eventStream, WaitResult<T> result)
        {
            connection = eventStream.Listen(t =>
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
}

#endif