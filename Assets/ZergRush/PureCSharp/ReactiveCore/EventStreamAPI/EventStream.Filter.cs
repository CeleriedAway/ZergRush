using System;

namespace ZergRush.ReactiveCore
{
    public static partial class StreamApi
    {
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

        public static IEventStream<T2> FilterCast<T, T2>(this IEventStream<T> eventStream) where T2 : class
        {
            return eventStream.Filter(t => t is T2).Map(t => t as T2);
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

        
        public static IEventStream<T> Where<T>(this IEventStream<T> stream, Func<T, bool> predicate)
        {
            return stream.Filter(predicate);
        }
    }
}