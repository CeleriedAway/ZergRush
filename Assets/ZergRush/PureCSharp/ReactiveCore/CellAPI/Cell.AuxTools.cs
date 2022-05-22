using System;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        /// Unfortunately I didn't found a good way to implement Hold in anonymous cell style yet
        /// If implement it in usual way then if eventStream is fired before subscription then its value is lost
        /// So we need to subscribe right now and sink connection to lambda.
        public static ICell<T> Hold<T>(this IEventStream<T> eventStream, T initial, Action<IDisposable> connectionSink)
        {
            var cell = new Cell<T>(initial);
            connectionSink(eventStream.Subscribe(val => cell.value = val));
            return cell;
        }

        /// Makes connection to cell and creates another cell as intermediate buffer.
        /// It can be used for optimization purposes when you need multiple connections to complex cell
        /// you can materialize it to travers inner complex cell structure only once.
        public static Cell<T> Materialize<T>(this ICell<T> cell, IConnectionSink connectionSink)
        {
            var materializedCell = new Cell<T>();
            connectionSink.AddConnection(cell.Bind(val => materializedCell.value = val));
            return materializedCell;
        }
    }
}