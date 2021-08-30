using System;
using UnityEngine;
using ZergRush;

public struct IgnoreConnection
{
    public static IgnoreConnection operator +(IgnoreConnection connections, IDisposable connection)
    {
        return default;
    }
}

public class ConnectableMonoBehaviour : MonoBehaviour, IConnectionSink
{
    public Connections connections = new Connections();
    public IgnoreConnection ignored
    {
        get => new IgnoreConnection();
        set {}
    }
    public IConnectionSink connectionSink => this;

    public void DisconnectAll()
    {
        connections.DisconnectAll();
    }

    protected virtual void OnDestroy()
    {
        DisconnectAll();
    }

    public void AddConnection(IDisposable connection)
    {
        connections.addConnection = connection;
    }
}