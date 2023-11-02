using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace ZergRush.ReactiveCore
{
    //// <summary>
    ////     Cell presents a reactive value that is changed over time.
    ////     In any point of time it has current value and you can always listen for its updates.
    ////     It's name comes from anologue of cells in spreadsheets, where cell's value can depend on other cells.
    //// </summary>
    [Serializable, DebuggerDisplay("content: {value}")]
    public class Cell<T> : ICellRW<T>, IConnectable
    {
        //[SerializeField]
        private T val;
        [NonSerialized] protected EventStream<T> up;

        public Cell(T t)
        {
            val = t;
        }

        public Cell()
        {
        }


        public ref T valueRef => ref val;

        public T value
        {
            get { return val; }
            set
            {
                if (up != null && EqualityComparer<T>.Default.Equals(value, val) == false)
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

        public IEventStream changed => updates;

        public EventStream<T> updates
        {
            get { return up = up ?? new EventStream<T>(); }
        }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (up == null) up = new EventStream<T>();
            return up.Subscribe(callback);
        }

        public IDisposable OnChanged(Action action)
        {
            if (up == null) up = new EventStream<T>();
            return this.up.Subscribe(_ => action());
        }

        public override string ToString()
        {
            return value != null ? value.ToString() : "null";
        }

        public void SetValue(T v)
        {
            this.value = v;
        }

        public int getConnectionCount => up == null ? 0 : up.getConnectionCount;
    }
}