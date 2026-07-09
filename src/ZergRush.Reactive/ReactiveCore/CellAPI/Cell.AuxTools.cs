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
        
        // This version of Hold works well only when cell is subscribed right after creation
        // If not subscribed right away then it will return initial value until subscription and first event
        public static ICell<T> HoldWhenSubscribed<T>(this IEventStream<T> eventStream, T initial)
        {
            var cell = new HoldCell<T>(eventStream, initial);
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

        /// Creates special intermediate buffer for cell, that activates on first connection.
        /// It can be used for optimization purposes when you need multiple connections to complex cell
        /// Similar to Materialize but lazy.
        public static ICell<T> Cache<T>(this ICell<T> cell)
        {
            return new BufferCell<T>(cell);
        }

        public static ICell<T> ToStaticCell<T>(this T val)
        {
            return new StaticCell<T>(val);
        }

        class BufferCell<T> : BufferCellTransform<T>
        {
            ICell<T> source;
            public BufferCell(ICell<T> source)
            {
                this.source = source;
            }
            protected override IDisposable StartListenAndRefill()
            {
                return source.ListenUpdates(val => buffer.value = val);
            }
            protected override void RefillRaw()
            {
                buffer.value = source.value;
            }
        }
        
        class HoldCell<T> : BufferCellTransform<T>
        {
            IEventStream<T> eventStream;
            public HoldCell(IEventStream<T> eventStream, T initial)
            {
                buffer.value = initial;
                this.eventStream = eventStream;
            }

            protected override IDisposable StartListenAndRefill()
            {
                return eventStream.Subscribe(val => buffer.value = val);
            }

            protected override void RefillRaw()
            {
            }
        }
    }
}