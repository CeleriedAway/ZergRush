using System;

public interface IConnectionSink
{
    void AddConnection(IDisposable connection);
}
