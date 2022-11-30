using System.Collections.Generic;
using UnityEngine;
using ZergRush;

public static class CodeGenToolsUnity
{
    public static void CompareCheck<T>(this T t, T other) where T : ICompareChechable<T>
    {
        t.CompareCheck(other, new ZRCompareCheckHelper(), Debug.LogError);
    }
    
    public static ulong CalculateHash<T>(this T t) where T : IHashable
    {
        return t.CalculateHash(new ZRHashHelper());
    }
}