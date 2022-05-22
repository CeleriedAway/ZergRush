using System;
using System.Diagnostics;

namespace ZergRush.ReactiveCore
{
    [DebuggerDisplay("{value}")]
    public class AnonymousCell<T> : ICell<T>
    {
        public Func<Action<T>, IDisposable> listen;
        public Func<T> current;

        public T value
        {
            get { return current(); }
        }

        public AnonymousCell(Func<Action<T>, IDisposable> subscribe, Func<T> current)
        {
            this.listen = subscribe;
            this.current = current;
        }

        public IDisposable ListenUpdates(Action<T> reaction)
        {
            return listen(reaction);
        }

        public IDisposable OnChanged(Action action)
        {
            return listen(_ => action());
        }
    }
}