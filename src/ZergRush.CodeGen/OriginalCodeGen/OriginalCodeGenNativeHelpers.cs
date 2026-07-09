using System;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush.CodeGen
{
    public static class OriginalCodeGenNativeHelpers
    {
        public static bool Valid(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public static T Best<T>(this IEnumerable<T> collection, Func<T, float> score)
        {
            var best = default(T);
            var bestScore = float.MinValue;
            foreach (var item in collection)
            {
                var itemScore = score(item);
                if (itemScore > bestScore)
                {
                    bestScore = itemScore;
                    best = item;
                }
            }

            return best;
        }

        public static T TakeFirst<T>(this IList<T> list)
        {
            var value = list[0];
            list.RemoveAt(0);
            return value;
        }

        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public static bool TryFind<T>(this IEnumerable<T> collection, Func<T, bool> predicate, out T value)
        {
            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    value = item;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static TValue TryGetOrNew<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = new TValue();
                dictionary[key] = value;
            }

            return value;
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }

        public static void EnsureSizeWithNulls<T>(this List<T> list, int count) where T : class
        {
            while (list.Count < count)
            {
                list.Add(null);
            }
        }

        public static bool AddIfNotContains<T>(this IList<T> list, T item)
        {
            if (list.Contains(item)) return false;
            list.Add(item);
            return true;
        }
    }
}
