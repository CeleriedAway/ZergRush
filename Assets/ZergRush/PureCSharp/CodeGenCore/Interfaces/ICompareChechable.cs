using System.Collections.Generic;

public interface ICompareChechable<in T>
{
    void CompareCheck(T t, Stack<string> path);
}