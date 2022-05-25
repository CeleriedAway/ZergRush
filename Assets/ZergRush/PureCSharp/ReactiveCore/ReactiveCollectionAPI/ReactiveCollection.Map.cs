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
                base.RefillBuffer();
                return collection.update.Subscribe(Process);
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