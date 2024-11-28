using System;
using System.Collections.Generic;
using System.Linq;
using ZergRush;

namespace ZergRush.CodeGen
{
    public static class ViewModelTools
    {
        public static void GentleReset<T>(this IList<T> self, IList<T> other) where T : class
        {
            var i = 0;
            for (; i < other.Count; i++)
            {
                var currOtherItem = other[i];
                if (currOtherItem == null)
                {
                    if (self.Count > i)
                    {
                        if (self[i] == null)
                        {
                            continue;
                        }
                        else
                        {
                            self.Insert(i, null);
                            continue;
                        }
                    }
                }

                int selfMatchingItemIndex = -1;
                var selfCount = self.Count;
                for (int j = i; j < selfCount; j++)
                {
                    var stableIdentifiable = self[j];
                    if (stableIdentifiable != null && ReferenceEquals(stableIdentifiable, currOtherItem))
                    {
                        selfMatchingItemIndex = j;
                        break;
                    }
                }

                check:
                if (selfMatchingItemIndex == i)
                {
                    // self is here, do nothing
                }
                else if (selfMatchingItemIndex > i)
                {
                    var currSelfItem = self[i];
                    // check if current self item is absent in other to make the most painless shift
                    var otherPosOfCurrentSelf = other.IndexOf(o => ReferenceEquals(o, currSelfItem));
                    if (otherPosOfCurrentSelf == -1)
                    {
                        // this self is redundant, remove, shift index and go again
                        self.RemoveAt(i);
                        selfMatchingItemIndex--;
                        goto check;
                    }

                    // move item to right position
                    var selfItem = self.TakeAt(selfMatchingItemIndex);
                    self.Insert(i, selfItem);
                }
                else if (selfMatchingItemIndex == -1)
                {
                    self.Insert(i, currOtherItem);
                }
                else
                {
                    throw new ZergRushException("found self in the past, this should not happer");
                }
            }

            var toRemove = self.Count - i;
            // remove redundant self items if any
            for (int j = 0; j < toRemove; j++)
            {
                self.RemoveAt(self.Count - 1);
            }
        }

        public static void UpdateFromCollection<T, T2>(this IList<T> buffer, IEnumerable<T2> collection)
            where T : IUpdatableFrom<T2>, new()
        {
            if (collection == null)
            {
                buffer.Clear();
                return;
            }

            var coll = collection as List<T2> ?? collection.ToList();
            int oldCnt = buffer.Count;
            while (buffer.Count > coll.Count)
            {
                buffer.TakeLast();
            }

            for (int i = 0; i < coll.Count; i++)
            {
                var inst = new T();
                inst.UpdateFrom(coll[i], new ZRUpdateFromHelper());
                if (i < oldCnt)
                {
                    buffer[i] = inst;
                }
                else
                {
                    buffer.Add(inst);
                }
            }
        }

        public static void UpdateFromCollection<T, T2>(this IList<T> buffer, IEnumerable<T2> collection,
            Func<T2, T> createFactory, Action<T2, T> destroyFactory)
            where T : IUpdatableFrom<T2>
        {
            if (collection == null)
            {
                buffer.Clear();
                return;
            }

            var coll = collection as List<T2> ?? collection.ToList();

            while (buffer.Count > coll.Count)
            {
                buffer.TakeLast();
            }

            for (int i = buffer.Count; i < coll.Count; i++)
            {
                buffer.Add(createFactory(coll[i]));
            }

            for (int i = 0; i < coll.Count; i++)
            {
                buffer[i].UpdateFrom(coll[i], new ZRUpdateFromHelper());
            }
        }
    }
}