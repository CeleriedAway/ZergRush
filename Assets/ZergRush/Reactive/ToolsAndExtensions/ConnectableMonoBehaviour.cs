using System;
using UnityEngine;
using ZergRush;

public class ConnectableMonoBehaviour : MonoBehaviour, IConnectionSink
{
    public Connections connections = new Connections();

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