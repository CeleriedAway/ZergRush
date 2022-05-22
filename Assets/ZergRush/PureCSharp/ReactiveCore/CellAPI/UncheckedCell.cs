using System;
using System.Diagnostics;

namespace ZergRush.ReactiveCore
{
    [Serializable]
    [DebuggerDisplay("{value}")]
    // Does not do equation check on value assignment
    public sealed class UncheckedCell<T> : ICellRW<T>
    {
        //[SerializeField]
        private T val;
        [NonSerialized] private EventStream<T> up;

        public UncheckedCell(T t)
        {
            val = t;
        }

        public UncheckedCell()
        {
        }

        public T value
        {
            get { return val; }
            set
            {
                val = value;
                if (up != null) up.Send(val);
            }
        }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (up == null) up = new EventStream<T>();
            return up.Subscribe(callback);
        }
    }
}