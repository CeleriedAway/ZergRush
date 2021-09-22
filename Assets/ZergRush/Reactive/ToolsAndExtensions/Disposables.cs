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

    public class AnonymousDisposable : IDisposable
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

        public bool disposed = false;

        void AlertIfDisposed() {if (disposed) throw new ZergRushException("double disposable is disposed");}
        public IDisposable First
        {
            get { AlertIfDisposed(); return first; }
            set { first = value;AlertIfDisposed(); }
        }

        public IDisposable Second
        {
            get { AlertIfDisposed(); return second; }
            set { AlertIfDisposed(); second = value; }
        }

        public void Dispose()
        {
            if (First != null) First.Dispose();
            if (Second != null) Second.Dispose();
            disposed = true;
        }
    }
    public class MultipleDisposable : IDisposable
    {
        List<IDisposable> items = new List<IDisposable>();
        public bool disposed;

        public void Add(IDisposable disposable)
        {
            if (disposed) throw new ZergRushException("add on disposed object");
            items.Add(disposable);
        }
        public void Dispose()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].Dispose();
                }
            }
            items.Clear();
            disposed = true;
        }
    }

    public class Connections : List<IDisposable>, IDisposable, IConnectionSink
    {
        public string tag;
        
        public Connections()
        {
        }

        public Connections(int capacity) : base(capacity)
        {
        }

        public Connections(IDisposable connection)
            => Add(connection);

        public IDisposable addConnection
        {
            set => Add(value);
        }

        public static Connections operator +(Connections connections, IDisposable connection)
        {
            if (connection == null)
                return connections;
            if (connections == null)
                connections = new Connections();
            connections.Add(connection);
            return connections;
        }

        public static Connections operator +(Connections connections, Action disposeAction)
        {
            if (disposeAction == null)
                return connections;
            if (connections == null)
                connections = new Connections();
            connections.Add(new AnonymousDisposable(disposeAction));
            return connections;
        }
        
        public void RemoveAndDisposeConnection(IDisposable item)
        {
            item.Dispose();
            if (!Remove(item))
            {
                UnityEngine.Debug.LogError("this connection not found");
            }
        }

        public virtual void Dispose()
        {
            this.DisconnectAll();
        }

        public Action<IDisposable> connectionSink
        {
            get { return disp => addConnection = disp; }
        }

        public void AddConnection(IDisposable connection)
        {
            Add(connection);
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

        public static void DisposeSafe(this IDisposable connection) { 
            DisconnectSafe(connection);
        }

        public static void DisconnectSafe(this IDisposable connection)
        {
            if (connection == null) return;
            connection.Dispose();
        }

        public static void AddTo(this IDisposable connection, List<IDisposable> connectionList)
        {
            connectionList.Add(connection);
        }
    }

    public class CellJoinDisposable<T> : DoubleDisposable
    {
        public T lastValue;
    }
    public class CellMergeMultipleDisposable<T> : MultipleDisposable
    {
        public CellMergeMultipleDisposable() : base() { }
        public T lastValue;
    }
}