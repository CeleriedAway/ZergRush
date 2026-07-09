using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZergRush;

namespace ZergRush.ReactiveCore
{
    public static class ReactiveCoreNativeHelpers
    {
        public static string PrintCollection<T>(this IEnumerable<T> collection, string delimiter = ", ")
        {
            if (collection == null) return "empty collection";
            return string.Join(delimiter, collection.Select(val => val?.ToString()).ToArray());
        }

        public static string ToError(this Exception exception)
        {
            if (exception == null) return string.Empty;

            var builder = new StringBuilder();
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (builder.Length > 0) builder.AppendLine();
                builder.Append(current.GetType().FullName).Append(": ").AppendLine(current.Message);
                builder.AppendLine(current.StackTrace);
            }

            return builder.ToString();
        }

        public static TValue TakeKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            var value = dictionary[key];
            dictionary.Remove(key);
            return value;
        }

        public static T TakeAt<T>(this IList<T> list, int index)
        {
            var value = list[index];
            list.RemoveAt(index);
            return value;
        }

        public static int UpperBound<T>(this IList<T> list, T value) where T : IComparable<T>
        {
            var low = 0;
            var high = list.Count;
            while (low < high)
            {
                var mid = low + ((high - low) >> 1);
                if (value.CompareTo(list[mid]) >= 0) low = mid + 1;
                else high = mid;
            }

            return low;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T value)
        {
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < list.Count; i++)
            {
                if (comparer.Equals(list[i], value)) return i;
            }

            return -1;
        }

        public static T Find<T>(this IReadOnlyList<T> list, Func<T, bool> predicate, T ifNotFound = default)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (predicate(item)) return item;
            }

            return ifNotFound;
        }

        public static TCast FindCast<T, TCast>(this IReadOnlyList<T> list) where TCast : class
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] is TCast value) return value;
            }

            return null;
        }

        public static T LastElement<T>(this IReadOnlyList<T> list, T ifNoElements = default)
        {
            return list.Count == 0 ? ifNoElements : list[list.Count - 1];
        }

        public static void RemoveLast<T>(this ReactiveCollection<T> collection)
        {
            collection.RemoveAt(collection.Count - 1);
        }

        public static void Resize<T>(this ReactiveCollection<T> collection, int size, Func<int, T> createItem, Action<T> removeItem)
        {
            while (collection.Count > size)
            {
                var item = collection[collection.Count - 1];
                collection.RemoveAt(collection.Count - 1);
                removeItem?.Invoke(item);
            }

            while (collection.Count < size)
            {
                collection.Add(createItem(collection.Count));
            }
        }

        public static int InsertSorted<T>(this IList<T> list, Func<T, T, int> comparison, T item)
        {
            var low = 0;
            var high = list.Count;
            while (low < high)
            {
                var mid = low + ((high - low) >> 1);
                if (comparison(list[mid], item) <= 0) low = mid + 1;
                else high = mid;
            }

            list.Insert(low, item);
            return low;
        }

        public static int InsertSorted<T>(this IList<T> list, IComparer<T> comparer, T item)
        {
            return list.InsertSorted(comparer.Compare, item);
        }
    }
}
