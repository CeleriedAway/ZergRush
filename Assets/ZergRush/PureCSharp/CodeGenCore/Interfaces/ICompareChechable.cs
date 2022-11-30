using System;
using System.Collections.Generic;
using ZergRush;

public interface ICompareChechable<in T>
{
    void CompareCheck(T t, ZRCompareCheckHelper __helper, Action<string> printer);
}