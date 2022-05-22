using System;

namespace ZergRush.ReactiveCore
{
    public class AnonymousEventStream : IEventStream
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

}