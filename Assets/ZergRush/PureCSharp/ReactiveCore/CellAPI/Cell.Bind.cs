using System;
using JetBrains.Annotations;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        /// Calls action with current value of a cell and subscribes to its updates with that actionConfig.
        /// Declares a strong projection between data and its effect
        /// The most commongly used function in cell API
        [MustUseReturnValue("In most cases you should use returned value to disconnect from cell later")]
        public static IDisposable Bind<T>(this ICell<T> cell, Action<T> action)
        {
            var disp = cell.ListenUpdates(action);
            action(cell.value);
            return disp;
        }

        /// Alternative API to easier connections collection
        public static void Bind<T>(this ICell<T> cell, IConnectionSink connectionSink, Action<T> action)
        {
            connectionSink.AddConnection(cell.ListenUpdates(action));
            action(cell.value);
        }

        /// Alternative API to easier connections collection
        public static void ListenUpdates<T>(this ICell<T> cell, IConnectionSink connectionSink, Action<T> action)
        {
            connectionSink.AddConnection(cell.ListenUpdates(action));
        }

        /// Gets cell update stream as an event
        public static IEventStream<T> UpdateStream<T>(this ICell<T> cell)
        {
            if (cell is Cell<T>) return (cell as Cell<T>).updates;
            return new AnonymousEventStream<T>(cell.ListenUpdates);
        }
        
        /// Subscribe to changes without caring about the value
        public static IDisposable OnChanged<T>(this ICell<T> cell, Action action)
        {
            return cell.ListenUpdates(_ => action());
        }

        /// You can make bindings with each value of cell with connections container
        /// that will be disposed on next value change
        public static IDisposable AffectEachValue<T>(this ICell<T> val, Action<T, Connections> changes)
        {
            var changeCollection = new Connections();
            return new DoubleDisposable
            {
                First = val.Bind(v =>
                {
                    changeCollection.Dispose();
                    changes(v, changeCollection);
                }),
                Second = changeCollection
            };
        }

    }
}