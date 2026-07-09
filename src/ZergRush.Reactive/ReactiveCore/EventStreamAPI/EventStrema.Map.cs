using System;

namespace ZergRush.ReactiveCore
{
    public static partial class StreamApi
    {
        /// Transforms stream value with a function.
        public static IEventStream<T2> Map<T, T2>(this IEventStream<T> eventStream, Func<T, T2> map)
        {
            return new AnonymousEventStream<T2>(
                reaction => { return eventStream.Subscribe(val => reaction(map(val))); });
        }
        
        /// Transforms stream value with a function.
        public static IEventStream<T2> Map<T2>(this IEventStream eventStream, Func<T2> map)
        {
            return new AnonymousEventStream<T2>(reaction => { return eventStream.Subscribe(() => reaction(map())); });
        }

    }
}