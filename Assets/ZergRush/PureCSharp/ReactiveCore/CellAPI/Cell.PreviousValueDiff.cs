using System;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        /// With this function you receive previous cell value as second argument, first time its the same value.
        public static IDisposable BufferBind<T>(this ICell<T> cell, Action<T, T> action)
        {
            // Implicit lambda boxing used as a prev val storage here
            T prevVal = cell.value;
            return cell.Bind(v =>
            {
                action(v, prevVal);
                prevVal = v;
            });
        }

        /// With this function you receive previous cell value as second argument
        public static IDisposable BufferListenUpdates<T>(this ICell<T> cell, Action<T, T> action)
        {
            // Implicit lambda boxing used as a prev val storage here
            var prevVal = cell.value;
            return cell.ListenUpdates(v =>
            {
                action(v, prevVal);
                prevVal = v;
            });
        }

        /// Useful when you need previous value of a cell, it comes as a second item in the tuple.
        public static IEventStream<(T newValue, T oldValue)> BufferPreviousValue<T>(this ICell<T> cell)
        {
            // Implicit lambda boxing used as a prev val storage here
            var prevVal = cell.value;
            return new AnonymousEventStream<(T, T)>(action =>
            {
                return cell.ListenUpdates(v =>
                {
                    action((v, prevVal));
                    prevVal = v;
                });
            });
        }

        public static IEventStream<int> Delta(this ICell<int> cell)
        {
            return cell.BufferPreviousValue().Map(i => i.Item1 - i.Item2);
        }

        public static IEventStream<float> Delta(this ICell<float> cell)
        {
            return cell.BufferPreviousValue().Map(i => i.Item1 - i.Item2);
        }

        public static IDisposable BindDiff(this ICell<float> cell, Action<float> action)
        {
            // Implicit lambda boxing used as a prev val storage here
            float prevVal = cell.value;
            return cell.Bind(v =>
            {
                action(v - prevVal);
                prevVal = v;
            });
        }

        public static ICell<float> Diff(this ICell<float> cell)
        {
            // Implicit lambda boxing used as a prev val storage here
            float prevVal = cell.value;
            return new AnonymousCell<float>(action =>
            {
                return cell.Bind(v =>
                {
                    action(v - prevVal);
                    prevVal = v;
                });
            }, () => cell.value - prevVal);
        }
    }
}