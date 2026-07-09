using System.Collections.Generic;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        /// Creates a new cell that is updated from previous cell unless gate is closed (false),
        /// if that is so it waits gate to be true, to update its value from initial cell
        public static ICell<T> Gate<T>(this ICell<T> cell, ICell<bool> gate, IConnectionSink connectionSink)
        {
            var result = new Cell<T>(cell.value);
            connectionSink.AddConnection(cell.ListenUpdates(v =>
            {
                if (gate.value) result.value = v;
            }));
            connectionSink.AddConnection(gate.ListenUpdates(v =>
            {
                if (v) result.value = cell.value;
            }));
            return result;
        }

        /// Creates a new event that is updated from previous event unless gate is closed (false),
        /// when gate opens (true), all blocked events are instantly fired 
        public static IEventStream<T> Gate<T>(this IEventStream<T> e, ICell<bool> gate, IConnectionSink connectionSink)
        {
            var events = new List<T>();
            var newE = new EventStream<T>();
            connectionSink.AddConnection(e.Subscribe(v =>
            {
                if (gate.value) newE.Send(v);
                else events.Add(v);
            }));
            connectionSink.AddConnection(gate.ListenUpdates(v =>
            {
                if (!v) return;
                foreach (var @event in events)
                {
                    newE.Send(@event);
                }
            }));
            return newE;
        }
    }
}