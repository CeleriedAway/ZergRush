using System;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush
{
    public static class RandomExtensions
    {
        public static List<float> NormalizeFloatRange(this IEnumerable<float> range)
        {
            var list = range.ToList();
            var magnitude = list.Sum();
            if (magnitude == 0) return list;
            for (int i = 0; i < list.Count; i++)
            {
                list[i] /= magnitude;
            }
            return list;
        }

        public static T WeightedRandomElement<T>(this IEnumerable<T> coll, Func<T, float> weightFunc, System.Random generator)
        {
            if (coll.Any())
                return coll.ElementAt(GetRandomIndexFromWeghts(coll.Select(weightFunc), generator));
            else
                return default(T);
        }

        public static T WeightedRandomElement<T>(this IEnumerable<T> coll, System.Random generator,
            Func<T, float> weightFunc)
        {
            var list = coll.ToList();
            if (list.Count > 0)
                return list.ElementAt(GetRandomIndexFromWeghts(list.Select(weightFunc), generator));
            else
                return default(T);
        }

        public static int GetRandomIndexFromWeghts(this IEnumerable<float> probabilities, System.Random generator)
        {
            var tempProb = NormalizeFloatRange(probabilities);

            double diceRoll = generator.NextDouble();
            double accumulated = 0.0f;

            for (int i = 0; i < tempProb.Count; i++)
            {
                if (diceRoll >= accumulated && diceRoll <= accumulated + tempProb[i])
                {
                    return i;
                }
                accumulated += tempProb[i];
            }

            return 0;
        }

#if UNITY_5_3_OR_NEWER
        public static IEnumerable<T> RandomElements<T>(this List<T> list, int count)
        {
            return RandomNonoverlappedIndices(list.Count, count).Select(i => list[i]);
        }
        
        public static IEnumerable<T> RandomElements<T>(this T[] list, int count)
        {
            return RandomNonoverlappedIndices(list.Length, count).Select(i => list[i]);
        }
        
        public static T RandomElement<T>(this IEnumerable<T> list)
        {
            return list.ToList().RandomElement();
        }

        public static T RandomElement<T>(this ICollection<T> list)
        {
            if (list.Count < 1)
                return default(T);

            return list.ElementAt(UnityEngine.Random.Range(0, list.Count));
        }
        
        public static int GetRandomIndexFromWeghts(this IEnumerable<float> probabilities)
        {
            var tempProb = NormalizeFloatRange(probabilities);

            float diceRoll = UnityEngine.Random.value;
            float accumulated = 0.0f;

            for (int i = 0; i < tempProb.Count; i++)
            {
                if (diceRoll >= accumulated && diceRoll <= accumulated + tempProb[i])
                {
                    //Debug.Log("Random rolled " + diceRoll.ToString());
                    return i;
                }
                accumulated += tempProb[i];
            }

            return 0;
        }

        public static T GetRandomEnum<T>()
        {
            var possibilities = Enum.GetNames(typeof(T)).ToList();
            return (T) Enum.Parse(typeof(T), possibilities[UnityEngine.Random.Range(0, possibilities.Count)]);
        }
        
        public static int[] RandomNonoverlappedIndices(int max, int count)
        {
            if (max <= count)
            {
                int[] r = new int[max];
                for (int i = 0; i < max; i++)
                {
                    r[i] = i;
                }
                return r;
            }
            int[] result = new int[count];
            var range = Enumerable.Range(0, max).ToList();
            for (int i = 0; i < count; ++i)
            {
                int randIndex = UnityEngine.Random.Range(0, max - i);
                int rand = range[randIndex];
                result[i] = rand;
                range[randIndex] = range[max - i - 1];
            }

            return result;
        }
#endif
    }
}