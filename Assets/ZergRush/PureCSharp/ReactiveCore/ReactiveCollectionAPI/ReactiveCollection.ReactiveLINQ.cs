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
        
        public static ICell<bool> AllReactive<T>(this IReactiveCollection<T> collection,
            Func<T, bool> item)
        {
            return collection.AsCell().Map(c => c.All(item));
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
            Func<T, bool> item, T ifNotFound = default)
        {
            return collection.AsCell().Map(c => c.Find(item, ifNotFound));
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

        public static ICell<T> LastElementCell<T>(this IReactiveCollection<T> collection, T ifNoElements = default)
        {
            return collection.AsCell().Map(c => c.LastElement(ifNoElements));
        }

        public static void ReverseInPlace<T>(this ReactiveCollection<T> col)
        {
            for (var index = 0; index < col.Count / 2; index++)
                (col[index], col[col.Count - index - 1]) = (col[col.Count - index - 1], col[index]);
        }
        
        public static IReactiveCollection<T> TakeReactive<T>(this IReactiveCollection<T> collection, int count)
        {
            return new TakeReactiveCollection<T>(collection, new StaticCell<int>(count));
        }
        
        public static IReactiveCollection<T> TakeReactive<T>(this IReactiveCollection<T> collection, ICell<int> count)
        {
            return new TakeReactiveCollection<T>(collection, count);
        }

        class TakeReactiveCollection<T> : AbstractCollectionTransform<T>
        {
            public readonly IReactiveCollection<T> collection;
            public readonly ICell<int> count;

            public TakeReactiveCollection(IReactiveCollection<T> collection, ICell<int> count)
            {
                this.collection = collection;
                this.count = count;
            }

            protected override IDisposable StartListenAndRefill()
            {
                var disp = new DoubleDisposable();
                disp.First = collection.update.Subscribe(e =>
                {
                    switch (e.type)
                    {
                        case ReactiveCollectionEventType.Reset:
                            RefillRaw();
                            break;
                        case ReactiveCollectionEventType.Insert:
                            if (e.position >= count.value) return;
                            buffer.Insert(e.position, e.newItem);
                            if (count.value < collection.Count)
                            {
                                buffer.RemoveLast();
                            }
                            break;
                        case ReactiveCollectionEventType.Remove:
                            if (e.position >= count.value) return;
                            if (count.value < collection.Count)
                            {
                                buffer.Insert(count.value, collection[count.value]);
                            }
                            buffer.RemoveAt(e.position);
                            break;
                        case ReactiveCollectionEventType.Set:
                            if (e.position >= count.value) return;
                            buffer[e.position] = e.newItem;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
                disp.Second = count.ListenUpdates(c =>
                {
                    while (c < buffer.Count)
                    {
                        buffer.RemoveLast();
                    }
                    while (c > buffer.Count && c <= collection.Count)
                    {
                        buffer.Add(collection[buffer.Count]);
                    }
                });
                RefillRaw();
                return disp;
            }

            protected override void RefillRaw()
            {
                buffer.Reset(collection.Take(Math.Min(collection.Count, count.value)));
            }
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