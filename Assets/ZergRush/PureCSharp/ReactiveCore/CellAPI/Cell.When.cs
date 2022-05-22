using System;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        /// Maps cell value to object
        /// Cast cell type.
        /// Return special stream that guarantee to call listen function once filter is returned true
        /// So if filter return true on initial cell value listen function will be called right now.
        public static IEventStream WhenOnce<T>(this ICell<T> cell, Func<T, bool> filter)
        {
            return new AnonymousEventStream((Action reaction) =>
            {
                if (filter(cell.value))
                {
                    reaction();
                    return EmptyDisposable.value;
                }
                else
                {
                    var disp = new SingleDisposable();
                    disp.Disposable = cell.ListenUpdates(val =>
                    {
                        if (!filter(val)) return;
                        reaction();
                        disp.Dispose();
                    });
                    return disp;
                }
            });
        }

        /// Stream is called once when cell value is true
        public static IEventStream WhenOnce(this ICell<bool> cell)
        {
            return new AnonymousEventStream(reaction =>
            {
                if (cell.value)
                {
                    reaction();
                    return EmptyDisposable.value;
                }
                else
                {
                    var disp = new SingleDisposable();
                    disp.Disposable = cell.ListenUpdates(val =>
                    {
                        if (!val) return;
                        reaction();
                        disp.Dispose();
                    });
                    return disp;
                }
            });
        }

        /// Result stream will be called when cell value satisfy the predicate,
        /// next call will be when value changed to not satisfy predicate and then to satisfy predicate again.
        public static IEventStream When<T>(this ICell<T> cell, Func<T, bool> filter)
        {
            return new AnonymousEventStream(reaction =>
            {
                var disp = new MapDisposable<bool>();
                disp.Disposable = cell.Bind(val =>
                {
                    if (disp.last && !filter(val))
                    {
                        disp.last = false;
                    }
                    else if (!disp.last && filter(val))
                    {
                        disp.last = true;
                        reaction();
                    }
                });
                return disp;
            });
        }

        public static IEventStream WhenEqualsOnce<T>(this ICell<T> cell, T value)
        {
            return cell.WhenOnce(v => v.Equals(value));
        }

        /// Result stream will be calles each time cell value updates and satisfy predicate.
        public static IEventStream WhenUpdatedToSatisfy<T>(this ICell<T> cell, Func<T, bool> filter)
        {
            return new AnonymousEventStream((Action reaction) =>
            {
                return cell.ListenUpdates(val =>
                {
                    if (!filter(val)) return;
                    reaction();
                });
            });
        }

        public static IEventStream When(this ICell<bool> cell)
        {
            return cell.When(i => i);
        }

        public static IEventStream WhenMoreOrEqual(this ICell<float> cell, float value)
        {
            return cell.When(v => v >= value);
        }

        public static IEventStream WhenMoreOrEqual(this ICell<int> cell, int value)
        {
            return cell.When(v => v >= value);
        }

        public static IEventStream WhenUpdatedToTrue(this ICell<bool> cell)
        {
            return cell.WhenUpdatedToSatisfy(i => i);
        }

        public static IDisposable DoWhenTrue(this ICell<bool> condition, Func<IDisposable> disposableAction)
        {
            var disp = new DoubleDisposable();
            disp.Second = condition.Bind(v =>
            {
                if (v)
                {
                    disp.First = disposableAction();
                }
                else
                {
                    disp.First.DisconnectSafe();
                    disp.First = null;
                }
            });
            return disp;
        }
    }
}