using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZergRush.CodeGen;

public static class DefaultErrorLog
{
    static DefaultErrorLog()
    {
        InitErrorLog();
    }

    public static void InitErrorLog()
    {
        ErrorLogSink.errLog = Debug.LogError;
    }
}
