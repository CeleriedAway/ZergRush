using System;

public interface IConnectionSink
{
    void AddConnection(IDisposable connection);

    public static IConnectionSink operator +(IConnectionSink connectionSink, IDisposable connection) {
        connectionSink?.AddConnection(connection);
        return connectionSink;
    }
}
