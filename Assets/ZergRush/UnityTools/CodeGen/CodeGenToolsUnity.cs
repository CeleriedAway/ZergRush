using System.Collections.Generic;
using UnityEngine;

public static class CodeGenToolsUnity
{
    public static void CompareCheck<T>(this T t, T other) where T : ICompareChechable<T>
    {
        t.CompareCheck(other, new Stack<string>(), Debug.LogError);
    }
}