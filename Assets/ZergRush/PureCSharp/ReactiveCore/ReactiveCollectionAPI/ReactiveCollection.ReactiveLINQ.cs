using System;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush.ReactiveCore
{
    public static partial class ReactiveCollectionAPI
    {
        public static ICell<IReadOnlyList<T>> AsCell<T>(this IReactiveCollection<T> collection)
        {
            return new AnonymousCell<IReadOnlyList<T>>(action =>
            {
                return collection.update.Subscribe(_ => { action(collection); });
            }, () => collection);
        }

        public static ICell<int> CountCell<T>(this IReactiveCollection<T> coll)
        {
            return coll.AsCell().Map(c => c.Count);
        }

        public static ICell<bool> ContainsReactive<T>(this IReactiveCollection<T> collection,
            T item)
        {
            return collection.AsCell().Map(c => c.Contains(item));
        }

        public static ICell<bool> AnyReactive<T>(this IReactiveCollection<T> collection,
            Func<T, bool> item)
        {
            return collection.AsCell().Map(c => c.Any(item));
        }
        
        public static ICell<bool> AnyReactive<T>(this IReactiveCollection<T> collection,
            Func<T, ICell<bool>> item)
        {
            return collection.Map(item).Join().AnyReactive(i => i);
        }

        public static ICell<T> FindReactive<T>(this IReactiveCollection<T> collection,
            Func<T, bool> item)
        {
            return collection.AsCell().Map(c => c.Find(item));
        }

        public static ICell<T2> FindCastReactive<T, T2>(this IReactiveCollection<T> collection) where T2 : class
        {
            return collection.AsCell().Map(c => c.FindCast<T, T2>());
        }

        public static ICell<T> AtIndex<T>(this IReactiveCollection<T> collection, int index, T ifNoElement = default)
        {
            return collection.AsCell().Map(coll => coll.Count > index ? coll[index] : ifNoElement);
        }

        public static ICell<T> AtIndex<T>(this IReactiveCollection<T> collection, ICell<int> index,
            T ifNoElement = default)
        {
            return index.FlatMap(v => collection.AtIndex(v, ifNoElement));
        }

        public static ICell<T> LastElementCell<T>(this IReactiveCollection<T> collection)
        {
            return collection.AsCell().Map(c => c.LastElement());
        }

        public static IReactiveCollection<T> Reverse<T>(this IReactiveCollection<T> original)
        {
            return original.AsCell().Map(list => list.GetReversed()).ToReactiveCollection();
        }

        public static void ReverseInPlace<T>(this ReactiveCollection<T> col)
        {
            for (var index = 0; index < col.Count / 2; index++)
                (col[index], col[col.Count - index - 1]) = (col[col.Count - index - 1], col[index]);
        }

        public static IReactiveCollection<T> EnumerateRange<T>(this ICell<int> cellOfElemCount, Func<int, T> fill)
        {
            return new ReactiveRange<T> { fill = fill, cellOfCount = cellOfElemCount };
        }
        
        class ReactiveRange<T> : AbstractCollectionTransform<T>
        {
            public Func<int, T> fill;
            public ICell<int> cellOfCount;

            protected override IDisposable StartListenAndRefill()
            {
                return cellOfCount.Bind(FillBuffer);
            }

            protected override void RefillRaw()
            {
                FillBuffer(cellOfCount.value);
            }

            void FillBuffer(int i)
            {
                while (buffer.Count != i)
                {
                    if (buffer.Count > i) buffer.RemoveAt(buffer.Count - 1);
                    else if (buffer.Count < i) buffer.Add(fill(buffer.Count));
                }
            }
        }

    }
}