using System;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush
{
    public static partial class Utils
    {
        public static TV TryGetOrNew<TK, TV>(this Dictionary<TK, TV> dict, TK key)
            where TV : new()
        {
            TV val;
            if (!dict.TryGetValue(key, out val))
            {
                val = new TV();
                dict[key] = val;
            }
            return val;
        }

        public static TV AtOrDefault<TK, TV>(this Dictionary<TK, TV> dict, TK key)
            where TV : new()
        {
            TV val;
            if (!dict.TryGetValue(key, out val))
            {
                return default(TV);
            }
            return val;
        }

        public static T Best<T>(this IEnumerable<T> coll, Func<T, float> predicate)
        {
            var best = default(T);
            var curr = float.MinValue;
            foreach (var v in coll)
            {
                var r = predicate(v);
                if (r > curr)
                {
                    curr = r;
                    best = v;
                }
            }
            return best;
        }

        public static T FirstFilteredOrFirst<T>(this IEnumerable<T> enumerable, Func<T, bool> filter)
        {
            var firstOrDefault = enumerable.FirstOrDefault(filter);
            if (EqualityComparer<T>.Default.Equals(firstOrDefault, default(T)))
            {
                return enumerable.First();
            }
            return firstOrDefault;
        }

        public static void ForeachWithIndices<T>(this IEnumerable<T> value, Action<T, int> act)
        {
            int ix = 0;
            foreach (var e in value)
            {
                act(e, ix);
                ix++;
            }
        }
        public static void ForeachWithIndices<T>(this List<T> value, Action<T, int> act)
        {
            for (var i = 0; i < value.Count; i++)
            {
                act(value[i], i);
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> list, T elem)
        {
            return list.IndexOf(val => EqualityComparer<T>.Default.Equals(val, elem));
        }

        public static int IndexOf<T>(this IEnumerable<T> list, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (var elem in list)
            {
                if (predicate(elem)) return index;
                index++;
            }
            return -1;
        }

        public static IEnumerable<T> Some<T>(params T[] elements)
        {
            return elements;
        }
        
        public static List<T> AddSome<T>(this List<T> list, int count, System.Func<T> elem)
        {
            for (int i = 0; i < count; ++i) list.Add(elem());
            return list;
        }

        public static void ZipIterate<T1, T2>(this IEnumerable<T1> coll1, IEnumerable<T2> coll2, Action<T1, T2> func)
        {
            var it1 = coll1.GetEnumerator();
            var it2 = coll2.GetEnumerator();
            while (it1.MoveNext() && it2.MoveNext())
            {
                func(it1.Current, it2.Current);
            }
        } 

        public static bool AddIfNotContains<T>(this IList<T> list, T item)
        where T : class
        {
            if (list.Contains(item)) return false;
            list.Add(item);
            return true;
        }

        public static bool AddIfNotContainsType<T>(this IList<T> list, T item)
        where T : class
        {
            foreach (var obj in list)
            {
                if (obj.GetType() == item.GetType())
                {
                    return false;
                }
            }
            list.Add(item);
            return true;
        }

        public static IEnumerable<T> Lift<T>(this T self)
        {
            yield return self;
        }

        public static void AddTo<TKey>(this Dictionary<TKey, float> dict, TKey key, float value)
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = 0;
            }
            dict[key] += value;
        }

        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue def)
        {
            return !dict.ContainsKey(key) ? def : dict[key];
        }


        public static T Take<T>(this List<T> list, int index)
        {
            T t = list[index];
            list.RemoveAt(index);
            return t;
        }

        public static T TakeLast<T>(this List<T> list)
        {
            var index = list.Count - 1;
            var t = list[index];
            list.RemoveAt(index);
            return t;
        }

        public static T LastElement<T>(this List<T> list, T ifNoElements = default(T))
        {
            return list.Count > 0 ? list[list.Count - 1] : ifNoElements;
        }
        
        public static string PrintCollection<T>(this IEnumerable<T> collection)
        {
            return String.Join(", ", collection.Select(val => val.ToString()).ToArray());
        }

        // Like c++ upper bound. Uses binary search on sorted list
        public static int UpperBound<T>(this List<T> list, T val)
        {
            int index = list.BinarySearch(val);
            if (index >= 0) return index;
            index = ~index;
            return index;
        }

        public static IEnumerable<T> GetEnumValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static void Resize<T>(this List<T> list, int count, Func<T> create, Action<T> destroy)
        {
            while (list.Count > count)
            {
                destroy(list.TakeLast());
            }
            while (list.Count < count)
            {
                list.Add(create());
            }
        }
    }
}