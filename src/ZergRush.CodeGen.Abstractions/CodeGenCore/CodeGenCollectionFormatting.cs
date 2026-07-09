using System;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush.CodeGen;

public static class CodeGenCollectionFormatting
{
    public static string ToCodeGenListString<T>(this IEnumerable<T> collection, string delimiter = ", ")
    {
        if (collection == null) return "empty collection";
        return string.Join(delimiter, collection.Select(val => val?.ToString()).ToArray());
    }

    public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
    {
        var index = 0;
        foreach (var item in collection)
        {
            if (predicate(item)) return index;
            index++;
        }

        return -1;
    }

    public static T TakeAt<T>(this IList<T> list, int index)
    {
        var value = list[index];
        list.RemoveAt(index);
        return value;
    }

    public static int InsertSorted<T>(this IList<T> list, T value, Func<T, int> order)
    {
        var valueOrder = order(value);
        var index = list.IndexOf(item => order(item) >= valueOrder);
        if (index < 0)
        {
            list.Add(value);
            return list.Count - 1;
        }

        list.Insert(index, value);
        return index;
    }

    public static void EnsureSize<T>(this List<T> list, int count) where T : new()
    {
        while (list.Count < count)
        {
            list.Add(new T());
        }
    }
}
