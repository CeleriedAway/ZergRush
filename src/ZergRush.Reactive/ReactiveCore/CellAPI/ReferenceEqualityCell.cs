using System;
using System.Diagnostics;

namespace ZergRush.ReactiveCore
{
    [Serializable, DebuggerDisplay("content: {value}")]
    // Normal cell but compares values as references without equals operator
    public class ReferenceEqualityCell<T> : ICellRW<T>, IConnectable where T : class
    {
        private T val;
        [NonSerialized] protected EventStream<T> up;

        public ReferenceEqualityCell(T t) { val = t; } 
        public ReferenceEqualityCell() {}

        public T value
        {
            get { return val; }
            set
            {
                if (up != null && ReferenceEquals(value, val) == false)
                {
                    val = value;
                    up.Send(val);
                }
                else
                {
                    val = value;
                }
            }
        }
        
        public EventStream<T> updates { get { return up = up ?? new EventStream<T>(); } }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (up == null) up = new EventStream<T>();
            return up.Subscribe(callback);
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public int getConnectionCount => up == null ? 0 : up.getConnectionCount;
    }
}