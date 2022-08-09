using System;

namespace ZergRush.CodeGen
{
    public static class LogSink
    {
        public static Action<string> errLog;
        public static Action<string> log;

        static LogSink()
        {
            #if UNITY_EDITOR || UNITY_2017_1_OR_NEWER
            log = UnityEngine.Debug.Log;
            errLog = UnityEngine.Debug.LogError;
            #else
            log = Console.WriteLine;
            errLog = Console.Error.WriteLine;
            #endif
        }
    }
}