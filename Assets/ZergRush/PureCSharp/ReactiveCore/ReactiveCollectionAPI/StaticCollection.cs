using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush.ReactiveCore
{
    public class StaticCollection<T> : IReactiveCollection<T>
    {
        public List<T> list;

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get { return AbandonedStream<ReactiveCollectionEvent<T>>.value; }
        }

        public List<T> current
        {
            get { return list; }
        }

        static readonly StaticCollection<T> def = new StaticCollection<T>{list = new List<T>()};
        public static IReactiveCollection<T> Empty()
        {
            return def;
        }

        public int Count => list.Count;

        public T this[int index] => list[index];
    }

    public static partial class ReactiveCollectionAPI
    {
        public static IReactiveCollection<T> ToStaticReactiveCollection<T>(this List<T> coll)
        {
            return new StaticCollection<T> { list = coll };
        }

        public static IReactiveCollection<T> ToStaticReactiveCollection<T>(this IEnumerable<T> coll)
        {
            return new StaticCollection<T> { list = coll.ToList() };
        }
        
        public static ReactiveCollection<T> ToReactiveCollection<T>(this IEnumerable<T> coll)
        {
            var r = new ReactiveCollection<T>();
            r.Reset(coll);
            return r;
        }

        public static List<T> ToList<T>(this ReactiveCollection<T> coll)
        {
            return coll.AsEnumerable().ToList();
        }
        
        class SingleItemCollection<T> : IReactiveCollection<T>
        {
            public ICell<T> item;
            public IEnumerator<T> GetEnumerator()
            {
                yield return item.value;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                yield return item.value;
            }

            public int Count => 1;
            public T this[int index]
            {
                get
                {
                    if (index == 0) return item.value;
                    throw new ArgumentOutOfRangeException($"Single item collection index out of range {index}");
                }
            }

            public IEventStream<IReactiveCollectionEvent<T>> update => item.BufferPreviousValue().Map(v =>
                new ReactiveCollectionEvent<T>
                {
                    newItem = v.Item1, position = 0, type = ReactiveCollectionEventType.Set, oldItem = v.Item2
                });
        }
        
        public static IReactiveCollection<T> ToSingleItemCollection<T>(this ICell<T> item)
        {
            return new SingleItemCollection<T> { item = item };
        }
        
        // if emptyPredicate returns true, collection will be empty
        public static IReactiveCollection<T> ToSingleItemCollection<T>(this ICell<T> item, Func<T, bool> emptyPredicate)
        {
            return new SingleItemCollectionWithPredicate<T> { item = item, emptyPredicate = emptyPredicate};
        }
        
        public static IReactiveCollection<T> ToSingleItemCollectionEmptyOnNull<T>(this ICell<T> item) where T : class
        {
            return new SingleItemCollectionWithPredicate<T> { item = item, emptyPredicate = v => v == null};
        }
        
        class SingleItemCollectionWithPredicate<T> : IReactiveCollection<T>
        {
            public ICell<T> item;
            public Func<T, bool> emptyPredicate;
            public IEnumerator<T> GetEnumerator()
            {
                if (emptyPredicate(item.value)) yield break;
                yield return item.value;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                if (emptyPredicate(item.value)) yield break;
                yield return item.value;
            }

            public int Count => emptyPredicate(item.value) ? 0 : 1;
            public T this[int index]
            {
                get
                {
                    if (index >= 1 || index < 0) throw new ArgumentOutOfRangeException($"Single item collection index out of range {index}");
                    if (emptyPredicate(item.value)) throw new ArgumentOutOfRangeException($"Single item collection is empty by predicate");
                    return item.value;
                }
            }

            public IEventStream<IReactiveCollectionEvent<T>> update => item.BufferPreviousValue().Map(v =>
            {
                var wasEmpty = emptyPredicate(v.oldValue);
                var nowEmpty = emptyPredicate(v.newValue);
                if (nowEmpty && wasEmpty) return null;
                if (wasEmpty && !nowEmpty) return new ReactiveCollectionEvent<T>
                {
                    newItem = v.newValue, position = 0, type = ReactiveCollectionEventType.Insert
                };
                if (!wasEmpty && nowEmpty) return new ReactiveCollectionEvent<T>
                {
                    oldItem = v.oldValue, position = 0, type = ReactiveCollectionEventType.Remove
                };
                return new ReactiveCollectionEvent<T>
                {
                    newItem = v.newValue, oldItem = v.oldValue, position = 0, type = ReactiveCollectionEventType.Set, 
                };
            }).Filter(i => i != null);
        }
    }

}