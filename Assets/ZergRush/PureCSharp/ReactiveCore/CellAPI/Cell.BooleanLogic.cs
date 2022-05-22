using System.Collections.Generic;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        public static ICell<bool> Not(this ICell<bool> value)
        {
            return value.Map(val => !val);
        }

        public static ICell<bool> And(this ICell<bool> value, ICell<bool> other)
        {
            return value.Merge(other, (b, b1) => b && b1);
        }

        public static ICell<bool> And(this ICell<bool> value, bool other)
        {
            return value.Map(b => b && other);
        }

        public static ICell<bool> Or(this ICell<bool> value, ICell<bool> other)
        {
            return value.Merge(other, (b, b1) => b || b1);
        }
        
        public static ICell<bool> Or(this ICell<bool> value, bool other)
        {
            return value.Map(b => b || other);
        }

        public static ICell<bool> ReactiveEquals<T>(this ICell<T> value, ICell<T> other)
        {
            return value.Merge(other, (b, b1) => EqualityComparer<T>.Default.Equals(b, b1));
        }
    }
}