using System;

namespace ZergRush.ReactiveCore
{
    /// <summary>
    ///     ICell
    ///     Represents a value that is changed over time.
    ///     In any point of time it has current value and you can always listen for its updates.
    ///     It's name comes from analogue of cells in spreadsheets, where cell's value can depend on other cells.
    /// </summary>
    public interface ICell<out T> 
    {
        IDisposable ListenUpdates(Action<T> reaction);
        T value { get; }
    }
    
    /// A value that can be read and written
    public interface IValueRW<T>
    {
        T value { get; set; }
    }

    /// A value that can be read, observed and written
    public interface ICellRW<T> : ICell<T>, IValueRW<T>
    {
        new T value { get; set; }
    }
    
    public interface IConnectable
    {
        int getConnectionCount { get; }
    }
}