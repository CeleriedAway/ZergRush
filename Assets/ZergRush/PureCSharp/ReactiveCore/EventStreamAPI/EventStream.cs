using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ZergRush.ReactiveCore
{
    public class EventStream<T> : IEventRW<T>, IConnectable
    {
        List<Action<T>> callbacks;
        bool iterating;
        ValueListItem nextValue;

        class ValueListItem
        {
            public T item;
            public List<Action<T>> callbacks;
            public ValueListItem next;
        }

        class Disconnect : IDisposable
        {
            public EventStream<T> reader;
            public Action<T> action;

            public void Dispose()
            {
                if (reader != null)
                {
                    reader.RemoveListener(action);
                    reader = null;
                    action = null;
                }
            }
        }

        void RemoveListener(Action<T> action)
        {
            // if (iterating)
            // {
            //     callbacks = callbacks.ToList();
            // }
            callbacks.Remove(action);
        }

        [MustUseReturnValue("In most cases you should use returned value to disconnect from event later")]
        public IDisposable Subscribe(Action<T> action)
        {
            if (callbacks == null) callbacks = new List<Action<T>>();
            else if (iterating) { callbacks = callbacks.ToList(); }
            callbacks.Add(action);
            return new Disconnect { reader = this, action = action };
        }

        public void Send(T t)
        {
            if (callbacks == null) return;

            if (iterating)
            {
                var newItem = new ValueListItem
                {
                    item = t,
                    callbacks = callbacks
                };
                if (nextValue == null)
                {
                    nextValue = newItem;
                }
                else
                {
                    var lastVal = nextValue;
                    while (lastVal.next != null) lastVal = lastVal.next;
                    lastVal.next = newItem;
                }
                return;
            }

            /// That is a protection from recursive Send() calls.
            iterating = true;

            var callbacksLocal = callbacks;

            iterateCallbacks:
            for (int i = 0; i < callbacksLocal.Count; i++)
            {
                callbacksLocal[i](t);
            }

            if (nextValue != null)
            {
                t = nextValue.item;
                callbacksLocal = nextValue.callbacks;
                nextValue = nextValue.next;
                goto iterateCallbacks;
            }

            iterating = false;
        }

        public IDisposable Subscribe(Action action)
        {
            if (callbacks == null) callbacks = new List<Action<T>>();
            Action<T> wrapper = _ => action();
            callbacks.Add(wrapper);
            return new Disconnect { reader = this, action = wrapper };
        }

        public int ConnectionsCount()
        {
            return callbacks != null ? callbacks.Count : 0;
        }

        public int getConnectionCount => callbacks == null ? 0 : callbacks.Count;
        public bool anybody => callbacks != null && callbacks.Count > 0;
    }

    //// Parametless variant of Event
    public class EventStream : IEventStream, IEventWriter, IConnectable
    {
        List<Action> callbacks;
        bool iterating;
        ValueListItem nextValue;

        class ValueListItem
        {
            public List<Action> callbacks;
            public ValueListItem next;
        }

        class Disconnect : IDisposable
        {
            public EventStream stream;
            public Action action;

            public void Dispose()
            {
                if (stream != null)
                {
                    stream.RemoveListener(action);
                    stream = null;
                    action = null;
                }
            }
        }

        void RemoveListener(Action action)
        {
            if (iterating)
            {
                callbacks = callbacks.ToList();
            }
            callbacks.Remove(action);
        }

        public void ClearCallbacks()
        {
            if (callbacks == null) return;
            if (iterating)
            {
                callbacks = callbacks.ToList();
            }
            callbacks.Clear();
        }

        [MustUseReturnValue("In most cases you should use returned value to disconnect from cell later")]
        public IDisposable Subscribe(Action action)
        {
            if (callbacks == null) callbacks = new List<Action>();
            else if (iterating) callbacks = callbacks.ToList();
            callbacks.Add(action);
            return new Disconnect {stream = this, action = action};
        }
        
        public static IDisposable operator+(EventStream stream, Action act)
        {
            return stream.Subscribe(act);
        }

        public void Send()
        {
            if (callbacks == null) return;

            if (iterating)
            {
                var newItem = new ValueListItem {callbacks = callbacks};
                if (nextValue == null)
                {
                    nextValue = newItem;
                }
                else
                {
                    ValueListItem lastVal = nextValue;
                    while (lastVal.next != null) lastVal = lastVal.next;
                    lastVal.next = newItem;
                }
                return;
            }

            iterating = true;

            var callbacksLocal = callbacks;

            iterateCallbacks:
            for (int i = 0; i < callbacksLocal.Count; i++)
            {
                callbacksLocal[i]();
            }

            if (nextValue != null)
            {
                callbacksLocal = nextValue.callbacks;
                nextValue = nextValue.next;
                goto iterateCallbacks;
            }

            iterating = false;
        }

        public int getConnectionCount => callbacks == null ? 0 : callbacks.Count;
    }
}