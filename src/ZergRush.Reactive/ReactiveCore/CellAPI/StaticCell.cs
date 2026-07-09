using System;
using System.Diagnostics;

namespace ZergRush.ReactiveCore
{
    public static class StaticCell
    {
        public static ICell<bool> False = new StaticCell<bool>(false);
        public static ICell<bool> True = new StaticCell<bool>(true);
    }

    [Serializable]
    [DebuggerDisplay("{value}")]
    public sealed class StaticCell<T> : ICell<T>
    {
        public StaticCell()
        {
        }

        public StaticCell(T initial)
        {
            val = initial;
        }

        //[SerializeField]
        readonly T val = default(T);

        public T value
        {
            get { return val; }
        }

        public IDisposable ListenUpdates(Action<T> reaction)
        {
            return EmptyDisposable.value;
        }

        public IDisposable OnChanged(Action action)
        {
            return EmptyDisposable.value;
        }

        static StaticCell<T> def = new StaticCell<T>();

        public static ICell<T> Default() => def;
    }
}