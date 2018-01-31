using System;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush.ReactiveCore
{
#if NET_4_6
    public interface IEventStream<out T> : IEventStream
#else
    public interface IEventStream<T> : IEventStream
#endif
    {
        IDisposable Listen(Action<T> action);
    }

    public interface IEventStream
    {
        IDisposable Listen(Action action);
    }

    public class EventStream<T> : IEventStream<T>
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
            public EventStream<T> stream;
            public Action<T> action;

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

        void RemoveListener(Action<T> action)
        {
            if (iterating)
            {
                callbacks = callbacks.ToList();
            }
            callbacks.Remove(action);
        }

        public IDisposable Listen(Action<T> action)
        {
            if (callbacks == null) callbacks = new List<Action<T>>();
            else if (iterating) callbacks = callbacks.ToList();
            callbacks.Add(action);
            return new Disconnect {stream = this, action = action};
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

        public IDisposable Listen(Action action)
        {
            if (callbacks == null) callbacks = new List<Action<T>>();
            Action<T> wrapper = _ => action();
            callbacks.Add(wrapper);
            return new Disconnect {stream = this, action = wrapper};
        }

        public int ConnectionsCount()
        {
            return callbacks != null ? callbacks.Count : 0;
        }
    }
    
    // Parametless variant of event stream.
    public class EventStream : IEventStream
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

        public IDisposable Listen(Action action)
        {
            if (callbacks == null) callbacks = new List<Action>();
            else if (iterating) callbacks = callbacks.ToList();
            callbacks.Add(action);
            return new Disconnect {stream = this, action = action};
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
    }

    class AbandonedStream : IEventStream
    {
        public IDisposable Listen(Action action)
        {
            return EmptyDisposable.value;
        }

        public static AbandonedStream value = new AbandonedStream();
    }

    class AbandonedStream<T> : IEventStream<T>
    {
        public IDisposable Listen(Action<T> action)
        {
            return EmptyDisposable.value;
        }

        public IDisposable Listen(Action action)
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

        public IDisposable Listen(Action observer)
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

        public IDisposable Listen(Action<T> observer)
        {
            return listen(observer);
        }

        public IDisposable Listen(Action observer)
        {
            return listen(_ => observer());
        }
    }

    public static class StreamApi
    {
        public static IEventStream<T> Filter<T>(this IEventStream<T> eventStream, Func<T, bool> filter)
        {
            return new AnonymousEventStream<T>(reaction =>
            {
                return eventStream.Listen(val =>
                {
                    if (filter(val)) reaction(val);
                });
            });
        }
        
        public static IEventStream Filter(this IEventStream eventStream, Func<bool> filter)
        {
            return new AnonymousEventStream(reaction =>
            {
                return eventStream.Listen(() =>
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
                return stream.Listen(v =>
                {
                    if (v) reaction();
                });
            });
        }

        // Transforms stream value with a function.
        public static IEventStream<T2> Map<T, T2>(this IEventStream<T> eventStream, Func<T, T2> map)
        {
            return new AnonymousEventStream<T2>(reaction => { return eventStream.Listen(val => reaction(map(val))); });
        }

        // Result stream is called only once, then the connection is disposed.
        public static IEventStream<T> Once<T>(this IEventStream<T> eventStream)
        {
            return new AnonymousEventStream<T>((Action<T> reaction) =>
            {
                var disp = new SingleDisposable();
                disp.Disposable = eventStream.Listen(val =>
                {
                    reaction(val);
                    disp.Dispose();
                });
                return disp;
            });
        }

        public static IEventStream Once(this IEventStream stream)
        {
            return new AnonymousEventStream((Action reaction) =>
            {
                var disp = new SingleDisposable();
                disp.Disposable = stream.Listen(() =>
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
                    disp.Add(other.Listen(reaction));
                }

                return disp;
            });
        }
        public static IEventStream Merge(params IEventStream[] others)
        {
            return new AnonymousEventStream((reaction) =>
            {
                var disp = new Connections(others.Length);

                for (var i = 0; i < others.Length; i++)
                {
                    var other = others[i];
                    disp.Add(other.Listen(reaction));
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
                disp.Add(stream.Listen(reaction));

                foreach (var other in others)
                {
                    disp.Add(other.Listen(reaction));
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
                disp.Add(stream.Listen(reaction));
                foreach (var other in others)
                {
                    disp.Add(other.Listen(reaction));
                }

                return disp;
            });
        }
    }
}