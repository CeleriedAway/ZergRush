using System;
using System.Collections.Generic;

namespace ZergRush
{
    public class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable value = new EmptyDisposable();

        public void Dispose()
        {
        }
    }

    class AnonymousDisposable : IDisposable
    {
        Action dispose;

        public AnonymousDisposable(Action dispose)
        {
            this.dispose = dispose;
        }

        public void Dispose()
        {
            if (dispose != null)
            {
                dispose();
                dispose = null;
            }
        }
    }

    public class SingleDisposable : IDisposable
    {
        IDisposable current;

        public bool IsDisposed
        {
            get { return current == null; }
        }

        public IDisposable Disposable
        {
            get { return current; }
            set
            {
                if (current != null)
                {
                    throw new InvalidOperationException("Disposable is already set");
                }
                current = value;
            }
        }

        public void Dispose()
        {
            if (current != null) current.Dispose();
            current = null;
        }
    }

    class MapDisposable<T> : SingleDisposable
    {
        public T last;
    }

    public class DoubleDisposable : IDisposable
    {
        public IDisposable first;
        public IDisposable second;

        public void Dispose()
        {
            first.Dispose();
            second.Dispose();
        }
    }

    public class Connections : List<IDisposable>, IDisposable
    {
        public Connections()
        {
        }

        public Connections(int capacity) : base(capacity)
        {
        }

        public IDisposable addConnection
        {
            set { Add(value); }
        }

        public void Dispose()
        {
            this.DisconnectAll();
        }

        public Action<IDisposable> connectionSink
        {
            get { return disp => addConnection = disp; }
        }
    }

    public class ListJoinDisposable<T> : Connections
    {
        public T lastValue;

        public ListJoinDisposable()
        {
        }

        public ListJoinDisposable(int capacity) : base(capacity)
        {
        }
    }

    public static class ConnectionCollection
    {
        public static void DisconnectAll(this List<IDisposable> connections)
        {
            if (connections == null) return;
            for (var i = 0; i < connections.Count; i++)
            {
                connections[i].Dispose();
            }
            connections.Clear();
        }

        public static void DisconnectSafe(this IDisposable connection)
        {
            if (connection == null) return;
            connection.Dispose();
        }
    }

    public class CellJoinDisposable<T> : DoubleDisposable
    {
        public T lastValue;
    }
}