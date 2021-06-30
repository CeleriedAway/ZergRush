#if UNITY_5_3_OR_NEWER

using System;

public interface IConnectionSink
{
    void AddConnection(IDisposable connection);
}

#endif