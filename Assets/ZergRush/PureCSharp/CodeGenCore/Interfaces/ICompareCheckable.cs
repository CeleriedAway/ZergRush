using System;
using System.Collections.Generic;
using ZergRush;

public interface ICompareCheckable<in T>
{
    void CompareCheck(T t, ZRCompareCheckHelper __helper, Action<string> printer);
}