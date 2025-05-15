﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZergRush
{
    public static partial class Utils
    {
        public static bool IsNullOrWhitespace(this string str)
        {
            return String.IsNullOrWhiteSpace(str);
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return String.IsNullOrEmpty(str);
        }

        public static bool Valid(this string str)
        {
            return !String.IsNullOrWhiteSpace(str);
        }
        
        public static int HashMix(int hash1, int hash2)
        {
            hash1 += hash2;
            hash1 += hash1 << 11;
            hash1 ^= hash1 >> 7;
            return hash1;
        }
        public static ulong HashMix(ulong hash1, ulong hash2)
        {
            hash1 += hash2;
            hash1 += hash1 << 11;
            hash1 ^= hash1 >> 7;
            return hash1;
        }
        
        #if !CONSOLE_GEN
        public static ulong CalculateHash(this ReadOnlySpan<char> array)
        {
            if (array == null) return 1234567;
            ulong hash = 0;
            for (int i = 0; i < array.Length; i++)
            {
                hash += array[i];
                hash += hash << 10;
                hash ^= hash >> 7;
            }

            return hash;
        }
        #endif

        public static ulong CalculateHash(this string array)
        {
            #if !CONSOLE_GEN
            ReadOnlySpan<char> span = array;
            return CalculateHash(span);
            #else
            return 0;
            #endif
            // if (array == null) return 1234567;
            // ulong hash = 0;
            // for (int i = 0; i < array.Length; i++)
            // {
            //     hash += array[i];
            //     hash += hash << 10;
            //     hash ^= hash >> 7;
            // }
            // return hash;
        }

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

        public static bool TryFind<T>(this IEnumerable<T> list, Func<T, bool> predicate, out T val)
        {
            foreach (var item in list)
            {
                if (predicate(item))
                {
                    val = item;
                    return true;
                }
            }

            val = default;
            return false;
        }

        public static T AtClampedIndex<T>(this T[] array, int index)
        {
            return array[index < 0 ? 0 : (index >= array.Length ? array.Length - 1 : index)];
        }

        public static T AtClampedIndex<T>(this List<T> array, int index)
        {
            return array[index < 0 ? 0 : (index >= array.Count ? array.Count - 1 : index)];
        }

        public static int InsertSorted<T>(this IList<T> list, T val, Func<T, int> order)
        {
            var orderNum = order(val);
            var i = list.IndexOf(t => order(t) >= orderNum);
            if (i != -1)
            {
                list.Insert(i, val);
                return i;
            }
            else
            {
                list.Add(val);
                return list.Count - 1;
            }
        }

        // returns insert position
        public static int InsertSorted<T>(this IList<T> list, Func<T, T, int> predicate, T val)
        {
            //TODO make logn
            var i = list.IndexOf(t => predicate(t, val) >= 0);
            if (i != -1)
            {
                list.Insert(i, val);
                return i;
            }
            else
            {
                list.Add(val);
                return list.Count - 1;
            }
        }

        public static void Check<TK, TV>(this Dictionary<TK, TV> dict, TK key)
            where TV : new()
        {
            if (dict.ContainsKey(key) == false) dict[key] = new TV();
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

        public static int BestIndex<T>(this IReadOnlyList<T> coll, Func<T, float> predicate)
        {
            var best = -1;
            var curr = Single.MinValue;
            for (var i = 0; i < coll.Count; i++)
            {
                var v = coll[i];
                var r = predicate(v);
                if (r > curr)
                {
                    curr = r;
                    best = i;
                }
            }

            return best;
        }

        public static (T, int) BestWithIndex<T>(this IEnumerable<T> coll, Func<T, float> predicate)
        {
            var best = default(T);
            var bestIndex = -1;
            var curr = Single.MinValue;
            var i = 0;
            foreach (var v in coll)
            {
                var r = predicate(v);
                if (r > curr)
                {
                    curr = r;
                    best = v;
                    bestIndex = i;
                }
                i++;
            }
            return (best, bestIndex);
        }
        
        public static T Best<T>(this IEnumerable<T> coll, Func<T, float> predicate)
        {
            var best = default(T);
            var curr = Single.MinValue;
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

        //double criterio check
        public static T Best<T>(this IEnumerable<T> coll, Func<T, (float, float)> predicate)
        {
            var best = default(T);
            var curr = (Single.MinValue, Single.MinValue);
            foreach (var v in coll)
            {
                var r = predicate(v);
                if (r.Item1 > curr.Item1)
                {
                    curr = r;
                    best = v;
                }
                else if (Math.Abs(r.Item1 - curr.Item1) < 1e-6 && r.Item2 > curr.Item2)
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

        public static int IndexOf<T>(this IEnumerable<T> list, int startIndex, Func<T, bool> predicate,
            int ifNotFound = -1)
        {
            int index = 0;
            foreach (var elem in list)
            {
                if (startIndex <= index && predicate(elem)) return index;
                index++;
            }

            return ifNotFound;
        }

        public static int IndexOf<T>(this IEnumerable<T> list, Func<T, bool> predicate, int ifNotFound = -1)
        {
            int index = 0;
            foreach (var elem in list)
            {
                if (predicate(elem)) return index;
                index++;
            }

            return ifNotFound;
        }

        public static int LastIndexOf<T>(this IEnumerable<T> list, Func<T, bool> predicate, int ifNotFound = -1)
        {
            int indexFound = -1;
            int index = 0;
            foreach (var elem in list)
            {
                if (predicate(elem)) indexFound = index;
                index++;
            }

            return indexFound != -1 ? indexFound : ifNotFound;
        }

        public static int UpperBoundIndex(this IEnumerable<int> stepsSorted, int val)
        {
            int index = 0;
            foreach (var elem in stepsSorted)
            {
                if (val < elem) return index;
                index++;
            }

            return index;
        }

        public static IEnumerable<T> Some<T>(params T[] elements)
        {
            return elements;
        }

        public static List<T> AddSome<T>(this List<T> list, int count, Func<T> elem)
        {
            for (int i = 0; i < count; ++i) list.Add(elem());
            return list;
        }

        public static List<T> AddSome<T>(this List<T> list, T item)
        {
            list.Add(item);
            return list;
        }

        public static (List<T1>, List<T1>) SplitByPredicate<T1>(this IEnumerable<T1> coll1, Func<T1, bool> func)
        {
            var lTrue = new List<T1>();
            var lFalse = new List<T1>();
            foreach (var x1 in coll1)
            {
                if (func(x1)) lTrue.Add(x1);
                else lFalse.Add(x1);
            }

            return (lTrue, lFalse);
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

        public static void ZipIterate<T1, T2>(this IEnumerable<T1> coll1, IEnumerable<T2> coll2,
            Action<T1, T2, int> func)
        {
            var it1 = coll1.GetEnumerator();
            var it2 = coll2.GetEnumerator();
            int i = 0;
            while (it1.MoveNext() && it2.MoveNext())
            {
                func(it1.Current, it2.Current, i);
                i++;
            }
        }

        public static bool AddIfNotNull<T>(this IList<T> list, T item)
        {
            if (item == null) return false;
            list.Add(item);
            return true;
        }
        public static bool AddIfNotContains<T>(this IList<T> list, T item)
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

        public static int RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> dict, Func<TKey, TValue, bool> filter)
        {
            List<TKey> keysToRemove = new List<TKey>();
            foreach (var item in dict)
            {
                if (filter(item.Key, item.Value))
                    keysToRemove.Add(item.Key);
            }

            for (int i = 0; i < keysToRemove.Count; i++)
                dict.Remove(keysToRemove[i]);
            return keysToRemove.Count;
        }

        public static TVal TakeKey<TKey, TVal>(this Dictionary<TKey, TVal> dict, TKey key)
        {
            if (dict.TryGetValue(key, out var val))
            {
                dict.Remove(key);
                return val;
            }

            return default;
        }

        public static T TakeOne<T>(this List<T> list, Func<T, bool> filter)
        {
            for (var index = 0; index < list.Count; index++)
            {
                var x = list[index];
                if (filter(x))
                {
                    list.RemoveAt(index);
                    return x;
                }
            }

            return default(T);
        }

        public static List<T> TakeAll<T>(this IList<T> list, Func<T, bool> predicate)
        {
            var result = new List<T>();
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var x1 = list[i];
                if (predicate(x1))
                {
                    result.Add(list.TakeAt(i));
                }
            }

            return result;
        }
        
        public static T TakeValue<TKey, T>(this IDictionary<TKey, T> dict, TKey key, T def = default)
        {
            if (dict.TryGetValue(key, out var value))
            {
                dict.Remove(key);
                return value;
            }
            return def;
        }
        
        public static T TakeItem<T>(this IList<T> list, T item)
        {
            var index = list.IndexOf(item);
            if (index == -1)
            {
                throw new ZergRushException($"Item {item} not found in list");
            }
            list.RemoveAt(index);
            return item;
        }

        public static T TakeAt<T>(this IList<T> list, int index)
        {
            var t = list[index];
            list.RemoveAt(index);
            return t;
        }
        
        public static void TakeAtIndexes<T>(this IList<T> list, IEnumerable<int> index, IList<T> result)
        {
            foreach (var i1 in index.OrderBy(i => -i))
            {
                result.Add(list.TakeAt(i1));
            }
        }

        public static T TakeLast<T>(this IList<T> list)
        {
            var index = list.Count - 1;
            var t = list[index];
            list.RemoveAt(index);
            return t;
        }
        
        public static List<T> TakeLastSome<T>(this IList<T> list, int count)
        {
            var result = new List<T>();
            for (int i = 0; i < count; i++)
            {
                result.Add(list.TakeLast());
            }
            return result;
        }

        public static List<T> TakeFirstSome<T>(this IList<T> list, int count)
        {
            var result = new List<T>();
            for (int i = 0; i < count; i++)
            {
                result.Add(list.TakeAt(0));
            }
            return result;
        }
        public static T TakeFirst<T>(this IList<T> list)
        {
            var t = list[0];
            list.RemoveAt(0);
            return t;
        }

        public static T TakeFirstSafe<T>(this IList<T> list, T ifNoElements = default)
        {
            if (list.Count == 0) return ifNoElements;
            var t = list[0];
            list.RemoveAt(0);
            return t;
        }
        
        public static void RemoveSwapBack<T>(this IList<T> list, int index)
        {
            var lastIndex = list.Count - 1;
            (list[lastIndex], list[index]) = (list[index], list[lastIndex]);
            list.RemoveAt(lastIndex);
        }

        public static void RemoveLast<T>(this IList<T> list)
        {
            if (list.Count > 0)
                list.RemoveAt(list.Count - 1);
        }

        public static T LastElement<T>(this IReadOnlyList<T> list, T ifNoElements = default(T))
        {
            return list.Count > 0 ? list[list.Count - 1] : ifNoElements;
        }

        public static T AtSafe<T>(this IReadOnlyList<T> list, int index, T ifNoElement = default(T))
        {
            return index < list.Count && index >= 0 ? list[index] : ifNoElement;
        }

        public static T LastElement<T>(this T[] list, T ifNoElements = default(T))
        {
            return list.Length > 0 ? list[list.Length - 1] : ifNoElements;
        }

        public static T DequeueSafe<T>(this Queue<T> q) => q.Count == 0 ? default : q.Dequeue();

        // Filter elements of specific type and cast enumerable to that type
        public static IEnumerable<T2> FilterCast<T, T2>(this IEnumerable<T> list)
        {
            return list.Where(e => e is T2).Cast<T2>();
        }
        
        public static IEnumerable<T> FilterCast<T>(this IEnumerable<object> list) where T : class
        {
            return list.Where(e => e is T).Cast<T>();
        }
        
        public static T2 FindCast<T2>(this IEnumerable<object> list) where T2 : class
        {
            return list.Find(e => e is T2) as T2;
        }
        
        public static T2 FindCast<T2>(this IEnumerable<object> list, Func<T2, bool> predicate) where T2 : class
        {
            return list.Find(e => e is T2 t2 && predicate(t2)) as T2;
        }

        public static T2 FindCast<T, T2>(this IEnumerable<T> list) where T2 : class
        {
            return list.Find(e => e is T2) as T2;
        }
        
        public static T2 FindCast<T, T2>(this IEnumerable<T> list, Func<T2, bool> predicate) where T2 : class
        {
            return list.Find(e => e is T2 t2 && predicate(t2)) as T2;
        }

        public static string PrintCollection<T>(this IEnumerable<T> collection, string delimiter = ", ")
        {
            if (collection == null) return "empty collection";
            return String.Join(delimiter, collection.Select(val => val?.ToString()).ToArray());
        }

        // Like c++ upper bound. Uses binary search on sorted list
        public static int UpperBound<T>(this List<T> list, T val)
        {
            int index = list.BinarySearch(val);
            if (index >= 0) return index;
            index = ~index;
            return index;
        }

        // Like c++ upper bound. Uses binary search on sorted list
        public static int UpperBound<T>(this List<T> list, IComparer<T> val) where T : class
        {
            int index = list.BinarySearch(null, val);
            if (index >= 0) return index;
            index = ~index;
            return index;
        }

        public static IEnumerable<T> GetEnumValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static IEnumerable<int> GetEnumValuesInt<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<int>();
        }

        public static bool IsPowerOfTwo(this int val)
        {
            if (val < 0) val = -val;
            return (val & (val - 1)) == 0;
        }

        public static IEnumerable<TEnum> DecomposeToBasicValues<TEnum>(this TEnum value)
        {
            return GetEnumValuesInt<TEnum>().Where(v => v.IsPowerOfTwo() && ((int) (object) value & v) != 0)
                .Cast<TEnum>();
        }


        public static void EnsureSizeWithNulls<T>(this List<T> list, int count) where T : class
        {
            while (list.Count < count)
            {
                list.Add(null);
            }
        }

        public static void EnsureSize<T>(this List<T> list, int count) where T : new()
        {
            while (list.Count < count)
            {
                list.Add(new T());
            }
        }

        public static void Resize<T>(this IList<T> list, int count) where T : new()
        {
            while (list.Count > count)
            {
                list.TakeLast();
            }

            while (list.Count < count)
            {
                list.Add(new T());
            }
        }

        public static void Resize<T>(this SimpleList<T> list, int count) where T : new()
        {
            while (list.Count > count)
            {
                list.TakeLast();
            }

            while (list.Count < count)
            {
                list.Add(new T());
            }
        }

        public static void Resize<T>(this IList<T> list, int count, Func<int, T> create, Action<T> destroy)
        {
            while (list.Count > count)
            {
                destroy(list.TakeLast());
            }

            while (list.Count < count)
            {
                list.Add(create(list.Count));
            }
        }
        public static void Resize<T>(this IList<T> list, int count, Func<T> create, Action<T> destroy)
        {
            Resize(list, count, _ => create(), destroy);
        }

        public static (V, int) MinWithIndex<V>(this IEnumerable<V> list, V baseVal = default)
        {
            int index = 0;
            int currMaxIndex = -1;
            var comparer = Comparer<V>.Default;

            foreach (var v in list)
            {
                if (comparer.Compare(v, baseVal) < 0)
                {
                    baseVal = v;
                    currMaxIndex = index;
                }

                index++;
            }

            return (baseVal, currMaxIndex);
        }

        public static (V, int) MaxWithIndex<V>(this IEnumerable<V> list, V baseVal = default)
        {
            int index = 0;
            int currMaxIndex = -1;
            var comparer = Comparer<V>.Default;

            foreach (var v in list)
            {
                if (comparer.Compare(v, baseVal) > 0)
                {
                    baseVal = v;
                    currMaxIndex = index;
                }

                index++;
            }

            return (baseVal, currMaxIndex);
        }

        public static (V, int) MaxWithIndex<T, V>(this IEnumerable<T> list, Func<T, V> getVal, V baseVal = default)
        {
            return list.Select(getVal).MaxWithIndex(baseVal);
        }

        public static (V, int) MinWithIndex<T, V>(this IEnumerable<T> list, Func<T, V> getVal, V baseVal = default)
        {
            return list.Select(getVal).MinWithIndex(baseVal);
        }


        public static bool Contains<T>(this T[] list, T t)
        {
            for (var i = 0; i < list.Length; i++)
            {
                var x1 = list[i];
                if (EqualityComparer<T>.Default.Equals(t, x1)) return true;
            }

            return false;
        }

        public static void RemoveAll<T>(this IList<T> list, Predicate<T> whatToRemove)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                bool found = whatToRemove(list[i]);
                if (found)
                    list.RemoveAt(i);
            }
        }

        public static void InsertSorted<T, C>(this IList<T> list, T item, Func<T, C> whatToCompare)
            where C : IComparable
        {
            C itemValue = whatToCompare(item);
            int minInd = 0;
            int maxInd = list.Count;
            while (minInd < maxInd)
            {
                int middleInd = (minInd + maxInd) / 2;
                C middleValue = whatToCompare(list[middleInd]);
                int compare = itemValue.CompareTo(middleValue);
                if (compare < 0)
                    maxInd = middleInd;
                else if (compare > 0)
                    minInd = middleInd + 1;
                else
                {
                    minInd = middleInd;
                    maxInd = middleInd;
                }
            }

            int insertInd = minInd;
            list.Insert(insertInd, item);
        }

        public static void Swap<T>(ref T item1, ref T item2)
        {
            T temp = item1;
            item1 = item2;
            item2 = temp;
        }

        public static bool RemoveOne<T>(this List<T> list, Predicate<T> whatToRemove)
        {
            for (int i = 0; i < list.Count; i++)
            {
                bool found = whatToRemove(list[i]);
                if (!found)
                    continue;
                list.RemoveAt(i);
                return true;
            }

            return false;
        }

        public static List<int> SelectRandomIndsFromWeights(this List<float> weights, int maxCount)
        {
            List<int> selectedInds = new List<int>();
            while (selectedInds.Count < maxCount && weights.Count > selectedInds.Count)
            {
                // Sum all not selectedTypeName weights.
                float sum = 0;
                for (int i = 0; i < weights.Count; i++)
                {
                    if (selectedInds.Contains(i))
                        continue;
                    sum += weights[i];
                }

                // Find next random ind.
                float rand = ZergRandom.global.Range(0, 1) * sum;
                int selectedInd = -1;
                for (int i = 0; i < weights.Count; i++)
                {
                    if (selectedInds.Contains(i))
                        continue;
                    rand -= weights[i];
                    if (rand <= 0)
                    {
                        selectedInd = i;
                        break;
                    }
                }

                if (selectedInd == -1)
                    throw new Exception("WTF");
                selectedInds.Add(selectedInd);
            }

            return selectedInds;
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> items, Func<T, Task> action)
        {
            foreach (var item in items)
                await action(item);
        }

        public static T[] With<T>(this T[] array, T newItem)
        {
            T[] res = new T[array.Length + 1];
            for (int i = 0; i < array.Length; i++)
                res[i] = array[i];
            res[array.Length] = newItem;
            return res;
        }

        public static T Min<T>(this IEnumerable<T> collection) where T : IComparable
        {
            T min = collection.FirstOrDefault();
            foreach (var item in collection)
            {
                if (item.CompareTo(min) < 0)
                    min = item;
            }

            return min;
        }

        public static T Max<T>(this IEnumerable<T> collection) where T : IComparable
        {
            T max = collection.FirstOrDefault();
            foreach (var item in collection)
            {
                if (item.CompareTo(max) > 0)
                    max = item;
            }

            return max;
        }

        public static IEnumerable<T2> MapAndFilterNulls<T, T2>(this IReadOnlyList<T> items, Func<T, T2> select)
            where T2 : class
        {
            return items.Select(select).Where(v => v != null);
        }

        public static List<T> Filter<T>(this List<T> items, Func<T, bool> filter)
        {
            List<T> filtered = new List<T>();
            foreach (var item in items)
            {
                if (filter(item))
                    filtered.Add(item);
            }

            return filtered;
        }

        public static List<T> Filter<T>(this T[] items, Func<T, bool> filter)
        {
            List<T> filtered = new List<T>();
            foreach (var item in items)
            {
                if (filter(item))
                    filtered.Add(item);
            }

            return filtered;
        }

        public static void ForEach<T>(this T[] array, Action<T> action)
        {
            if (array == null) return;
            for (int i = 0; i < array.Length; i++)
                action(array[i]);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (var item in source)
                action(item);
        }

        public static void ForEach<T>(this IList<T> list, Action<T> action)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
                action(list[i]);
        }

        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection list,
            Action<TValue> action)
        {
            if (list == null) return;
            foreach (var item in list)
                action(item);
        }

        public static T Find<T>(this IEnumerable<T> array, Func<T, bool> predicate, T def = default)
        {
            if (array == null) return default(T);
            foreach (var item in array)
            {
                if (predicate(item))
                    return item;
            }

            return def;
        }

        public static List<T> FindAll<T>(this T[] array, Func<T, bool> predicate)
        {
            List<T> res = new List<T>();
            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                    res.Add(array[i]);
            }

            return res;
        }

        public static IEnumerator<T> YieldToEnumerator<T>(this T t)
        {
            yield return t;
        }

        public static IEnumerable<T> Yield<T>(this T t)
        {
            yield return t;
        }

        public static IEnumerable<T> Yield<T>(params T[] t)
        {
            return t;
        }

        public static List<T> AddIfNotNull<T>(this List<T> list, T t) where T : class
        {
            if (t != null) list.Add(t);
            return list;
        }

        public static List<To> ConvertAll<From, To>(this IEnumerable<From> array, Func<From, To> convert)
        {
            List<To> res = new List<To>();
            foreach (var item in array)
                res.Add(convert(item));
            return res;
        }

        public static List<To> ConvertAll<From, To>(this From[] array, Func<From, To> convert)
        {
            List<To> res = new List<To>();
            for (int i = 0; i < array.Length; i++)
                res.Add(convert(array[i]));
            return res;
        }

        public static List<To> ConvertAll<To>(this Array array, Func<object, To> convert)
        {
            List<To> res = new List<To>();
            foreach (var item in array)
                res.Add(convert(item));
            return res;
        }

        public static List<T> GetReversed<T>(this IEnumerable<T> list)
        {
            List<T> res = new List<T>();
            foreach (var item in list)
                res.Insert(0, item);
            return res;
        }
    }
}