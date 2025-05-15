using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ZergRush.ReactiveCore
{
    public static partial class ReactiveCollectionAPI
    {
        public static IReactiveCollection<TMapped> Map<T, TMapped>(this IReactiveCollection<T> collection,
            Func<T, TMapped> mapFunc)
        {
            return new MappedCollection<T, TMapped>(collection, mapFunc);
        }
        
        public static ICell<int> SumReactive(this IReactiveCollection<int> collection)
        {
            return collection.AsCell().Map(c => c.Sum());
        }
        
        public static ICell<float> SumReactive(this IReactiveCollection<float> collection)
        {
            return collection.AsCell().Map(c => c.Sum());
        }
        
        public static ICell<int> SumReactive<T>(this IReactiveCollection<T> collection, Func<T, int> mapFunc)
        {
            return collection.Map(mapFunc).SumReactive();
        }
        
        public static ICell<float> SumReactive<T>(this IReactiveCollection<T> collection, Func<T, float> mapFunc)
        {
            return collection.Map(mapFunc).AsCell().Map(c => c.Sum());
        }
        
        public static ICell<int> SumReactive<T>(this IReactiveCollection<T> collection, Func<T, ICell<int>> mapFunc)
        {
            return collection.Map(mapFunc).Join().SumReactive();
        }
        
        public static ICell<float> SumReactive<T>(this IReactiveCollection<T> collection, Func<T, ICell<float>> mapFunc)
        {
            return collection.Map(mapFunc).Join().AsCell().Map(c => c.Sum());
        }
        
        public static IReactiveCollection<T> ReverseReactive<T>(this IReactiveCollection<T> collection)
        {
            return new ReversedCollection<T>(collection);
        }
        
        [DebuggerDisplay("{this.ToString()}")]
        class ReversedCollection<T> : AbstractCollectionTransform<T>
        {
            readonly IReactiveCollection<T> collection;

            public ReversedCollection(IReactiveCollection<T> collection)
            {
                this.collection = collection;
            }

            void Process(IReactiveCollectionEvent<T> e)
            {
                if (disconected) return;
                var reversedPos = buffer.Count - e.position;
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        RefillRaw(e.newData);
                        break;
                    case ReactiveCollectionEventType.Insert:
                        buffer.Insert(reversedPos, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        buffer.RemoveAt(reversedPos);
                        break;
                    case ReactiveCollectionEventType.Set:
                        buffer[reversedPos] = e.newItem;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                var disp = collection.update.Subscribe(Process);
                RefillBuffer();
                return disp;
            }

            protected void RefillRaw(IReadOnlyList<T> list)
            {
                buffer.Reset(list.Reverse());
            }
            protected override void RefillRaw()
            {
                RefillRaw(collection);
            }
        }

        [DebuggerDisplay("{this.ToString()}")]
        public class MappedCollection<T, TMapped> : AbstractCollectionTransform<TMapped>
        {
            readonly Func<T, TMapped> mapFunc;
            readonly IReactiveCollection<T> collection;

            public MappedCollection(IReactiveCollection<T> collection, Func<T, TMapped> mapFunc)
            {
                this.collection = collection;
                this.mapFunc = mapFunc;
            }

            void Process(IReactiveCollectionEvent<T> e)
            {
                if (disconected) return;

                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        RefillRaw(e.newData);
                        break;
                    case ReactiveCollectionEventType.Insert:
                        var item = mapFunc(e.newItem);
                        buffer.Insert(e.position, item);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        buffer.RemoveAt(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        var newItem = mapFunc(e.newItem);
                        buffer[e.position] = newItem;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                var disp = collection.update.Subscribe(Process);
                RefillBuffer();
                return disp;
            }

            protected void RefillRaw(IReadOnlyList<T> list)
            {
                buffer.Reset(list.Select(mapFunc));
            }
            protected override void RefillRaw()
            {
                RefillRaw(collection);
            }
        }
    }
}