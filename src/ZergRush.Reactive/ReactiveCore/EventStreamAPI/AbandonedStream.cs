using System;

namespace ZergRush.ReactiveCore
{
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

}