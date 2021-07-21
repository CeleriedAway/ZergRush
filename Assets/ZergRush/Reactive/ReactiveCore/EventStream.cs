using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ZergRush.ReactiveCore
{
    public interface IEventStream<out T> : IEventStream
    {
        IDisposable Subscribe(Action<T> action);
    }

    public interface IEventWriter<in T>
    {
        void Send(T val);
    }

    public interface IEventRW<T> : IEventStream<T>, IEventWriter<T>
    {
    }

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
            if (iterating)
            {
                callbacks = callbacks.ToList();
            }
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

            // That is a protection from recursive Send() calls.
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

    /// Parametless variant of IEventStream
    public interface IEventStream
    {
        IDisposable Subscribe(Action action);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IEventWriter
    {
        void Send();
    }
    
    /// <summary>
    /// Event that can be sent and observed
    /// </summary>
    public interface IEventRW : IEventStream, IEventWriter {}
    
    /// Parametless variant of Event
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

    public class AbandonedStream : IEventStream
    {
        public IDisposable Subscribe(Action action)
        {
            return EmptyDisposable.value;
        }

        public static AbandonedStream value = new AbandonedStream();
    }

    public class AbandonedStream<T> : IEventStream<T>
    {
        public IDisposable Subscribe(Action<T> action)
        {
            return EmptyDisposable.value;
        }

        public IDisposable Subscribe(Action action)
        {
            return EmptyDisposable.value;
        }

        public static AbandonedStream<T> value = new AbandonedStream<T>();
    }

    class AnonymousEventStream : IEventStream
    {
        readonly Func<Action, IDisposable> listen;

        public AnonymousEventStream(Func<Action, IDisposable> subscribe)
        {
            this.listen = subscribe;
        }

        public IDisposable Subscribe(Action observer)
        {
            return listen(observer);
        }
    }

    class AnonymousEventStream<T> : IEventStream<T>
    {
        readonly Func<Action<T>, IDisposable> listen;

        public AnonymousEventStream(Func<Action<T>, IDisposable> subscribe)
        {
            this.listen = subscribe;
        }

        public IDisposable Subscribe(Action<T> observer)
        {
            return listen(observer);
        }

        public IDisposable Subscribe(Action observer)
        {
            return listen(_ => observer());
        }
    }

    public static class StreamApi
    {
        public static void Subscribe<T>(this IEventStream<T> stream, IConnectionSink connectionSink, Action<T> action)
        {
            connectionSink.AddConnection(stream.Subscribe(action));
        }
        public static void Subscribe(this IEventStream e, IConnectionSink connectionSink, Action action)
        {
            connectionSink.AddConnection(e.Subscribe(action));
        }
        public static IEventStream<T> Filter<T>(this IEventStream<T> eventStream, Func<T, bool> filter)
        {
            return new AnonymousEventStream<T>(reaction =>
            {
                return eventStream.Subscribe(val =>
                {
                    if (filter(val)) reaction(val);
                });
            });
        }
        
        public static IEventStream Filter(this IEventStream eventStream, Func<bool> filter)
        {
            return new AnonymousEventStream(reaction =>
            {
                return eventStream.Subscribe(() =>
                {
                    if (filter()) reaction();
                });
            });
        }

        public static IEventStream<T> Where<T>(this IEventStream<T> stream, Func<T, bool> predicate)
        {
            return stream.Filter(predicate);
        }

        public static IEventStream WhenTrue(this IEventStream<bool> stream)
        {
            return new AnonymousEventStream(reaction =>
            {
                return stream.Subscribe(v =>
                {
                    if (v) reaction();
                });
            });
        }

        // Transforms stream value with a function.
        public static IEventStream<T2> Map<T, T2>(this IEventStream<T> eventStream, Func<T, T2> map)
        {
            return new AnonymousEventStream<T2>(reaction => { return eventStream.Subscribe(val => reaction(map(val))); });
        }
        // Transforms stream value with a function.
        public static IEventStream<T2> Map<T2>(this IEventStream eventStream, Func<T2> map)
        {
            return new AnonymousEventStream<T2>(reaction => { return eventStream.Subscribe(() => reaction(map())); });
        }

        // Result stream is called only once, then the connection is disposed.
        public static IEventStream<T> Once<T>(this IEventStream<T> eventStream)
        {
            return new AnonymousEventStream<T>((Action<T> reaction) =>
            {
                var disp = new SingleDisposable();
                disp.Disposable = eventStream.Subscribe(val =>
                {
                    reaction(val);
                    disp.Dispose();
                });
                return disp;
            });
        }
        
        public static IDisposable ListenWhile<T>(this IEventStream<T> stream, ICell<bool> listenCondition, Action<T> act)
        {
            var disp = new DoubleDisposable();
            disp.first = listenCondition.Bind(val =>
            {
                if (val)
                {
                    if (disp.disposed) return;
                    if (disp.second != null)
                    {
                        throw new ZergRushException();
                    }
                    disp.second = stream.Subscribe(act);
                }
                else if (disp.second != null)
                {
                    disp.second.Dispose();
                    disp.second = null;
                }
            });
            return disp;
        }

        public static IEventStream Once(this IEventStream stream)
        {
            return new AnonymousEventStream((Action reaction) =>
            {
                var disp = new SingleDisposable();
                disp.Disposable = stream.Subscribe(() =>
                {
                    reaction();
                    disp.Dispose();
                });
                return disp;
            });
        }

        // Merge an array of streams info one stream.
        public static IEventStream<T> Merge<T>(params IEventStream<T>[] others)
        {
            if (others == null || others.Any(s => s == null)) throw new ArgumentException("Null streams in merge");
            return new AnonymousEventStream<T>((reaction) =>
            {
                var disp = new Connections(others.Length);

                foreach (var other in others)
                {
                    disp.Add(other.Subscribe(reaction));
                }

                return disp;
            });
        }
        public static IEventStream Merge(this IEnumerable<IEventStream> others)
        {
            return MergeSome(others.ToArray());
        }
        public static IEventStream<T> Merge<T>(this IEnumerable<IEventStream<T>> events)
        {
            return new AnonymousEventStream<T>(reaction =>
            {
                var disp = new Connections();
                foreach (var other in events)
                {
                    disp.Add(other.Subscribe(reaction));
                }
                return disp;
            });
        }
        public static IEventStream MergeSome(params IEventStream[] others)
        {
            return new AnonymousEventStream((reaction) =>
            {
                var disp = new Connections(others.Length);

                for (var i = 0; i < others.Length; i++)
                {
                    var other = others[i];
                    disp.Add(other.Subscribe(reaction));
                }

                return disp;
            });
        }
        public static IEventStream<T> MergeWith<T>(this IEventStream<T> stream, params IEventStream<T>[] others)
        {
            if (stream == null || others == null || others.Any(s => s == null))
                throw new ArgumentException("Null streams in merge");
            return new AnonymousEventStream<T>((Action<T> reaction) =>
            {
                var disp = new Connections(others.Length + 1);
                disp.Add(stream.Subscribe(reaction));

                foreach (var other in others)
                {
                    disp.Add(other.Subscribe(reaction));
                }

                return disp;
            });
        }
        public static IEventStream MergeWith(this IEventStream stream, params IEventStream[] others)
        {
            if (stream == null || others == null || others.Any(s => s == null))
                throw new ArgumentException("Null streams in merge");
            return new AnonymousEventStream(reaction =>
            {
                var disp = new Connections(others.Length + 1);
                disp.Add(stream.Subscribe(reaction));
                foreach (var other in others)
                {
                    disp.Add(other.Subscribe(reaction));
                }

                return disp;
            });
        }
        static YieldAwaitable frame => Task.Yield();
        public static async Task<T> SingleMessageAsync<T>(this IEventStream<T> stream)
        {
            T result = default(T);
            bool finished = false;
            var waiting = stream.Subscribe(res => { result = res; finished = true; });
            while (!finished)
                await frame;
            return result;
        }
        public static async Task SingleMessageAsync(this IEventStream stream)
        {
            bool finished = false;
            var waiting = stream.Subscribe(() => { finished = true; });
            while (!finished)
                await frame;
        }
    }
}