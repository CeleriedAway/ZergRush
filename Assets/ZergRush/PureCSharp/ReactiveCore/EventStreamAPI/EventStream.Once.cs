using System;

namespace ZergRush.ReactiveCore
{
    public static partial class StreamApi
    {
        /// Result stream is called only once, then the connection is disposed.
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
    }
}