using System;

namespace ZergRush.ReactiveCore
{
    public static partial class StreamApi
    {
        public static void Subscribe<T>(this IEventStream<T> stream, IConnectionSink connectionSink, Action<T> action)
        {
            connectionSink.AddConnection(stream.Subscribe(action));
        }

        public static void Subscribe(this IEventStream e, IConnectionSink connectionSink, Action action)
        {
            connectionSink.AddConnection(e.Subscribe(action));
        }
        
        public static IDisposable SubscribeWhile<T>(this IEventStream<T> stream, ICell<bool> listenCondition, Action<T> act)
        {
            var disp = new DoubleDisposable();
            disp.First = listenCondition.Bind(val =>
            {
                if (val)
                {
                    if (disp.disposed) return;
                    if (disp.Second != null)
                    {
                        throw new ZergRushException();
                    }
                    disp.Second = stream.Subscribe(act);
                }
                else if (disp.Second != null)
                {
                    disp.Second.Dispose();
                    disp.Second = null;
                }
            });
            return disp;
        }

    }
}