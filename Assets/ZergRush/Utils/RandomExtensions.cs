using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush
{
    public static partial class RandomExtensions
    {
        public static T TakeRandom<T>(this List<T> list, ZergRandom random)
        {
            var i = random.Range(0, list.Count);
            return list.Take(i);
        }
        
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

        public static IEnumerable<T> RandomElements<T>(this IEnumerable<T> list, ZergRandom random, int count)
        {
            var l = list.ToList();
            return RandomNonoverlappedIndices(l.Count, count, random).Select(i => l[i]);
        }
        
        public static IEnumerable<T> RandomElements<T>(this List<T> list, ZergRandom random, int count)
        {
            return RandomNonoverlappedIndices(list.Count, count, random).Select(i => list[i]);
        }
        
        public static int MaskItem(this ZergRandom random, int mask)
        {
            var coll = new List<int>();
            for (int i = 0; i < 31; i++)
            {
                var l = 1 << i;
                if ((mask & l) != 0) coll.Add(l);
            }
            return coll.RandomElement(random);
        }
        
        public static T EnumMask<T>(this ZergRandom random, T mask)
        {
            int iMask = Convert.ToInt32(mask);
            if (iMask == 0)
            {
                foreach (var value in System.Enum.GetValues(typeof(T)))
                {
                    iMask |= (int) value;
                }
            }

            return (T) (object) MaskItem(random, iMask);
        }
        
        public static T Enum<T>(this ZergRandom random)
        {
            // fuck c#
            var vals = System.Enum.GetValues(typeof(T));
            return (T)vals.GetValue(random.Range(0, vals.Length));
        }

        public static List<T> Shuffle<T>(this IReadOnlyList<T> list, ZergRandom random) => list.RandomOrder(random);
        public static List<T> RandomOrder<T>(this IReadOnlyList<T> list, ZergRandom random)
        {
            var indexes = RandomNonoverlappedIndices(list.Count, list.Count, random);
            var result = new List<T>(list.Count);
            for (int i = 0; i < list.Count; i++) { result.Add(default(T)); }
            for (var i = 0; i < indexes.Length; i++)
            {
                var index = indexes[i];
                result[i] = list[index];
            }
            return result;
        }
        
        public static T RandomElement<T>(this IEnumerable<T> list, ZergRandom random)
        {
            return list.ToList().RandomElement(random);
        }

        public static T RandomElement<T>(this ICollection<T> list, ZergRandom random)
        {
            if (list.Count < 1) return default(T);

            return list.ElementAt(random.Range(0, list.Count));
        }

        public static List<T> RollSomeElements<T>(this IReadOnlyList<T> list, int count, ZergRandom random)
        {
            var result = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                result.Add(list[random.Range(0, list.Count)]);
            }
            return result;
        }
        
        // uniquenessCount == 1 means next element wont be same as element before
        public static List<T> RollSomeElementsAndGuaranteeLastUniqueness<T>(this IReadOnlyList<T> list, int count, int uniquenessCount, ZergRandom random)
        {
            var indexes = RandomNonoverlappedLastIndices(list.Count, count, uniquenessCount, random);
            if (indexes.Length != count)
            {
                throw new ZergRushException("internal error");
            }
            var result = new List<T>(count);
            for (var i = 0; i < indexes.Length; i++)
            {
                var index = indexes[i];
                result.Add(list[index]);
            }
            return result;
        }

        public static T RandomWeightedElement<T>(this List<T> elements, ZergRandom random, Func<T, int> weightFunc)
        {
            return RandomWeightedElement(elements, weightFunc, random, out _);
        }
        public static T RandomWeightedElement<T>(this List<T> elements, Func<T, int> weightFunc, ZergRandom random, out int index)
        {
            if (elements.Count == 1)
            {
                index = 0;
                return elements[0];
            }
            // Sum all not selectedTypeName weights.
            int sum = 0;
            for (int i = 0; i < elements.Count; i++)
            {
                sum += weightFunc(elements[i]);
            }
            // Find next random ind.
            int rand = random.Range(0, sum);
            int selectedInd = -1;
            for (int i = 0; i < elements.Count; i++)
            {
                rand -= weightFunc(elements[i]);
                if (rand <= 0)
                {
                    selectedInd = i;
                    break;
                }
            }
            if (selectedInd == -1) throw new ZergRushException("wtf");
            index = selectedInd;
            return elements[selectedInd];
        }
        
        // Func<int, T> gives you element form the end of the roll list
        public static List<T> RollSomeElementsAndGuaranteeLastUniqueness<T>(this IReadOnlyList<T> list, int count, 
            /*candidate element, all rolled elements tail sampler, rolled count*/
            Func<T, Func<int, T>, int, bool> uniquenessPredicate,
            ZergRandom random)
        {
            var indexes = RandomIndices(list.Count, count,
                (element, currIndexes, currCount) => 
                    uniquenessPredicate(list[element], tailIndex =>
                    {
                        return list[currIndexes[currCount - tailIndex - 1]];
                    }, currCount), random);
            if (indexes.Length != count)
            {
                throw new ZergRushException("internal error");
            }
            var result = new List<T>(count);
            for (var i = 0; i < indexes.Length; i++)
            {
                var index = indexes[i];
                result.Add(list[index]);
            }
            return result;
        }
        
        
        public static int[] RandomIndices(int max, int count, 
            /*candidate index, all rolled indexes, rolled count*/
            Func<int, int[], int, bool> uniquenessPredicate,
            ZergRandom random)
        {
            int[] result = new int[count];
            for (int i = 0; i < count; ++i)
            {
                randAgain:
                int randIndex = random.Range(max);
                if (!uniquenessPredicate(randIndex, result, i))
                {
                    goto randAgain;
                }
                result[i] = randIndex;
            }
            return result;
        }
        
        public static int[] RandomNonoverlappedLastIndices(int max, int count, int lastIndexUniqueness, ZergRandom random)
        {
            if (max <= lastIndexUniqueness) throw new ZergRushException("max is less then uniqueness");
            int[] result = new int[count];
            for (int i = 0; i < count; ++i)
            {
                randAgain:
                int randIndex = random.Range(max);
                if (i >= lastIndexUniqueness)
                    for (int j = 1; j <= lastIndexUniqueness; j++)
                    {
                        if (randIndex == result[i - j]) goto randAgain;
                    }
                result[i] = randIndex;
            }
            return result;
        }
        // max is exclusive index max, count is number of random indexes returned
        public static int[] RandomNonoverlappedIndices(int max, int count, ZergRandom random)
        {
            if (max < count)
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
                int randIndex = random.Range(0, max - i);
                int rand = range[randIndex];
                result[i] = rand;
                range[randIndex] = range[max - i - 1];
            }

            return result;
        }

        public static bool ChancePercent(this ZergRandom rand, int percent)
        {
            return rand.Range(100) < percent;
        }
        
        // chance is zero to one
        public static bool Chance(this ZergRandom rand, float chance)
        {
            return rand.Range(0f, 1f) < chance;
        }
        
        //percent is zero to 100
        public static bool ChancePercent(this ZergRandom rand, float percent)
        {
            return rand.NextDouble() * 100 < percent;
        }
        
        public static bool Bool(this ZergRandom rand)
        {
            return rand.Next() % 2 == 0;
        }
        
        public static int Range(this ZergRandom rand, int maxExcluding)
        {
            return Range(rand, 0, maxExcluding);
        }
        
        public static int Range(this ZergRandom rand, int min, int maxExcluding)
        {
            if (maxExcluding <= min) return min;
            return min + rand.Next() % (maxExcluding - min);
        }
        public static int RangeInclude(this ZergRandom rand, int min, int maxInclude)
        {
            if (maxInclude <= min) return min;
            return min + rand.Next() % (maxInclude - min + 1);
        }
        
        public static float Range(this ZergRandom rand, float min, float max)
        {
            if (max <= min) return min;
            return min + rand.NextFloat() * (max - min);
        }
    }
}