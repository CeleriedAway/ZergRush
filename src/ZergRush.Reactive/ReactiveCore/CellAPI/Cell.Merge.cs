using System;
using System.Collections.Generic;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        /// Merge two dynamic values into a tuple.
        public static ICell<(T, T2)> Merge<T, T2>(this ICell<T> cell, ICell<T2> cell2)
        {
            return Merge(cell, cell2, (v1, v2) => (v1, v2));
        }

        /// Merge three dynamic values into a tuple.
        public static ICell<(T, T2, T3)> Merge<T, T2, T3>(this ICell<T> cell, ICell<T2> cell2, ICell<T3> cell3)
        {
            return Merge(cell, cell2, cell3, (arg1, arg2, arg3) => (arg1, arg2, arg3));
        }

        public static ICell<(T, T2, T3, T4)> Merge<T, T2, T3, T4>(this ICell<T> cell, ICell<T2> cell2,
            ICell<T3> cell3, ICell<T4> cell4)
        {
            return Merge(cell, cell2, cell3, cell4, (arg1, arg2, arg3, arg4) => (arg1, arg2, arg3, arg4));
        }

        public static ICell<(T, T2, T3, T4, T5)> Merge<T, T2, T3, T4, T5>(this ICell<T> cell, ICell<T2> cell2,
            ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5)
        {
            return Merge(cell, cell2, cell3, cell4, cell5, (arg1, arg2, arg3, arg4, arg5) => (arg1, arg2, arg3, arg4, arg5));
        }

        public static ICell<(T, T2, T3, T4, T5, T6)> Merge<T, T2, T3, T4, T5, T6>(this ICell<T> cell,
            ICell<T2> cell2, ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5, ICell<T6> cell6)
        {
            return Merge(cell, cell2, cell3, cell4, cell5, cell6, (arg1, arg2, arg3, arg4, arg5, arg6) => (arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// Merge two dynamic values in new dynamic value with transformation function.
        public static ICell<TRes> Merge<T1, T2, TRes>(this ICell<T1> cell1, ICell<T2> cell2, Func<T1, T2, TRes> func)
        {
            Func<TRes> curr = () => func(cell1.value, cell2.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                return disp;
            }, curr);
        }

        public static ICell<TRes> Merge<T1, T2, T3, TRes>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3,
            Func<T1, T2, T3, TRes> func)
        {
            Func<TRes> curr = () => func(cell1.value, cell2.value, cell3.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                disp.Add(ListenUpdates(cell3, curr, disp, reaction));
                return disp;
            }, curr);
        }

        public static ICell<TRes> Merge<T1, T2, T3, T4, TRes>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3,
            ICell<T4> cell4, Func<T1, T2, T3, T4, TRes> func)
        {
            Func<TRes> curr = () => func(cell1.value, cell2.value, cell3.value, cell4.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                disp.Add(ListenUpdates(cell3, curr, disp, reaction));
                disp.Add(ListenUpdates(cell4, curr, disp, reaction));
                return disp;
            }, curr);
        }

        public static ICell<TRes> Merge<T1, T2, T3, T4, T5, TRes>(this ICell<T1> cell1, ICell<T2> cell2,
            ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5, Func<T1, T2, T3, T4, T5, TRes> func)
        {
            Func<TRes> curr = () => func(cell1.value, cell2.value, cell3.value, cell4.value, cell5.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                disp.Add(ListenUpdates(cell3, curr, disp, reaction));
                disp.Add(ListenUpdates(cell4, curr, disp, reaction));
                disp.Add(ListenUpdates(cell5, curr, disp, reaction));
                return disp;
            }, curr);
        }

        public static ICell<TRes> Merge<T1, T2, T3, T4, T5, T6, TRes>(this ICell<T1> cell1, ICell<T2> cell2,
            ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5, ICell<T6> cell6, Func<T1, T2, T3, T4, T5, T6, TRes> func)
        {
            Func<TRes> curr = () => func(cell1.value, cell2.value, cell3.value, cell4.value, cell5.value, cell6.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                disp.Add(ListenUpdates(cell3, curr, disp, reaction));
                disp.Add(ListenUpdates(cell4, curr, disp, reaction));
                disp.Add(ListenUpdates(cell5, curr, disp, reaction));
                disp.Add(ListenUpdates(cell6, curr, disp, reaction));
                return disp;
            }, curr);
        }
        
        static IDisposable ListenUpdates<T, TRes>(ICell<T> cell, Func<TRes> curr,
            CellMergeMultipleDisposable<TRes> disp, Action<TRes> reaction)
        {
            return cell.ListenUpdates(val =>
            {
                if (disp.disposed) return;
                TRes newCurr = curr();
                if (!EqualityComparer<TRes>.Default.Equals(newCurr, disp.lastValue))
                {
                    disp.lastValue = newCurr;
                    reaction(newCurr);
                }
            });
        }

        /// Bind with two cells in one call
        public static IDisposable MergeBind<T, T2>(this ICell<T> cell, ICell<T2> cell2, Action<T, T2> func)
        {
            return Merge(cell, cell2, (v1, v2) => (v1, v2)).Bind(val => func(val.Item1, val.Item2));
        }

        /// Bind with three cells in one call
        public static IDisposable MergeBind<T1, T2, T3>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3,
            Action<T1, T2, T3> func)
        {
            return Merge(cell1, cell2, cell3, (arg1, arg2, arg3) => (arg1, arg2, arg3))
                .Bind(val => func(val.Item1, val.Item2, val.Item3));
        }

        public static IDisposable MergeBind<T1, T2, T3, T4>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3,
            ICell<T4> cell4, Action<T1, T2, T3, T4> func)
        {
            return Merge(cell1, cell2, cell3, cell4, (arg1, arg2, arg3, arg4) => (arg1, arg2, arg3, arg4))
                .Bind(val => func(val.Item1, val.Item2, val.Item3, val.Item4));
        }

        public static IDisposable MergeBind<T1, T2, T3, T4, T5>(this ICell<T1> cell1, ICell<T2> cell2,
            ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5, Action<T1, T2, T3, T4, T5> func)
        {
            return Merge(cell1, cell2, cell3, cell4, cell5, (arg1, arg2, arg3, arg4, arg5) => (arg1, arg2, arg3, arg4, arg5))
                .Bind(val => func(val.Item1, val.Item2, val.Item3, val.Item4, val.Item5));
        }
    }
}