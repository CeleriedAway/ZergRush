using System;
using System.Collections;
using System.Collections.Generic;

namespace ZergRush.ReactiveCore
{
    public abstract class AbstractCollectionTransform<T> : IReactiveCollection<T>
    {
        protected bool disconected => connectionCounter == 0;
        
        int connectionCounter = 0;
        IDisposable collectionConnection;
        
        protected readonly ReactiveCollection<T> buffer = new ReactiveCollection<T>();
        
        bool connected
        {
            get { return connectionCounter != 0; }
        }

        void OnConnect()
        {
            if (connectionCounter == 0)
            {
                collectionConnection = StartListenAndRefill(); 
            }
            //Debug.Log($"connection counter increased to {connectionCounter} bufferCounter {buffer.connectionCount}");
            connectionCounter++;
        }

        protected abstract IDisposable StartListenAndRefill();

        void OnDisconnect()
        {
            connectionCounter--;
            //Debug.Log($"connection counter decreased to {connectionCounter} bufferCounter {buffer.connectionCount}");
            if (connectionCounter == 0)
            {
                if (buffer.getConnectionCount != 0)
                {
                    //Debug.LogError("WTF is that");
                    //throw new Exception("this should not happen");
                }
                collectionConnection.Dispose();
                collectionConnection = null;
                ClearBuffer();
            }
        }

        void ClearBuffer()
        {
            buffer.Reset();
        }

        protected void RefillBuffer()
        {
            RefillRaw();
        }
        
        protected abstract void RefillRaw();

        void RefillIfNotConnected()
        {
            if (!connected) RefillBuffer();
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            RefillIfNotConnected();
            return buffer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEventStream<IReactiveCollectionEvent<T>> updateWrapp;

        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get
            {
                return updateWrapp ??= new AnonymousEventStream<IReactiveCollectionEvent<T>>(act =>
                {
                    OnConnect();
                    var connection = buffer.update.Subscribe(act);

                    /// possible work around unity 2021 compilation crash
                    void Dispose()
                    {
                        connection.Dispose();
                        OnDisconnect();
                    }
                    var anonymousDisposable = new AnonymousDisposable(Dispose);
                    return anonymousDisposable;
                });
            }
        }

        public override string ToString()
        {
            return this.PrintCollection();
        }

        public int Count
        {
            get
            {
                RefillIfNotConnected();
                return buffer.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                RefillIfNotConnected();
                return buffer[index];
            }
        }
    }
}