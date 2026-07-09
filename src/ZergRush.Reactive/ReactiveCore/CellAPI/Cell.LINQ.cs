using System;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        /// Linq support
        public static ICell<T2> Select<T, T2>(this ICell<T> cell, Func<T, T2> selector)
        {
            return Map(cell, selector);
        }

        /// Linq support
        public static ICell<TR> SelectMany<T, TR>(this ICell<T> source, ICell<TR> other)
        {
            return SelectMany(source, _ => other);
        }

        /// Linq support
        public static ICell<TR> SelectMany<T, TR>(this ICell<T> source, Func<T, ICell<TR>> selector)
        {
            return source.FlatMap(selector);
        }

        /// Linq support
        public static ICell<TR> SelectMany<T, TC, TR>(this ICell<T> source, Func<T, ICell<TC>> collectionSelector,
            Func<T, TC, TR> resultSelector)
        {
            return source.SelectMany(x => collectionSelector(x).Select(y => resultSelector(x, y)));
        }
    }
}