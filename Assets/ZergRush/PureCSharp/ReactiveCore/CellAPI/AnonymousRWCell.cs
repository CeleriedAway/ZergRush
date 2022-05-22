using System;

namespace ZergRush.ReactiveCore
{
    public class AnonymousRWCell<T> : AnonymousCell<T>, ICellRW<T>
    {
        readonly Action<T> sink;

        public AnonymousRWCell(Func<Action<T>, IDisposable> subscribe, Func<T> current, Action<T> sink) :
            base(subscribe, current)
        {
            this.sink = sink;
        }

        public new T value
        {
            get { return base.value; }
            set { sink(value); }
        }
    }
}