using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        public static ICell<T2> Map<T, T2>(this ICell<T> cell, Func<T, T2> map)
        {
            if (cell == null) throw new ZergRushException($"Map cell of type {typeof(T)} is null");
            return new MappedCell<T, T2> { cell = cell, map = map };
        }

        public static ICell<T2> MapWithDefaultIfNull<T, T2>(this ICell<T> cell, Func<T, T2> map, T2 def = default)
            where T : class
        {
            return cell.Map(v => v == null ? def : map(v));
        }

        public static ICell<bool> Is<T>(this ICell<T> cell, T value)
        {
            return cell.Map(v => EqualityComparer<T>.Default.Equals(value, v));
        }

        public static ICell<bool> IsNot<T>(this ICell<T> cell, T value)
        {
            return cell.Map(v => EqualityComparer<T>.Default.Equals(value, v) == false);
        }

        public static ICell<int> Negate(this ICell<int> cell)
        {
            return cell.Map(v => -v);
        }

        public static ICell<float> Negate(this ICell<float> cell)
        {
            return cell.Map(v => -v);
        }

        public static ICell<object> AsObject<T>(this ICell<T> cell)
        {
            return cell.Select(val => val as object);
        }

        public static ICell<T2> Cast<T, T2>(this ICell<T> cell) where T2 : class
        {
            return cell.Select(val => val as T2);
        }
        
        [DebuggerDisplay("{value}")]
        sealed class MappedCell<T, T2> : ICell<T2>
        {
            public ICell<T> cell;
            public Func<T, T2> map;

            public IDisposable ListenUpdates(Action<T2> reaction)
            {
                var disp = new MapDisposable<T2>();
                disp.last = map(cell.value);
                disp.Disposable = cell.ListenUpdates(val =>
                {
                    var newCurr = map(val);
                    if (!EqualityComparer<T2>.Default.Equals(newCurr, disp.last))
                    {
                        disp.last = newCurr;
                        reaction(newCurr);
                    }
                });
                return disp;
            }

            public T2 value
            {
                get { return map(cell.value); }
            }
        }

    }
}