using System;

namespace ZergRush.ReactiveCore
{
    public interface IEventStream<out T> : IEventStream
    {
        IDisposable Subscribe(Action<T> action);
    }
    
    public interface IEventWriter<in T>
    {
        void Send(T val);
    }

    public interface IEventRW<T> : IEventStream<T>, IEventWriter<T>
    {
    }
    
    /// Parameterless variant of IEventStream
    public interface IEventStream
    {
        IDisposable Subscribe(Action action);
    }
    
    public interface IEventWriter
    {
        void Send();
    }
    
    /// Event that can be sent and observed
    public interface IEventRW : IEventStream, IEventWriter {}
    
    
}