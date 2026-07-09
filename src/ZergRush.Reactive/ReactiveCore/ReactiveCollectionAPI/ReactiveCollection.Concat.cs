using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ZergRush.ReactiveCore
{
    public static partial class ReactiveCollectionAPI
    {
        public static IReactiveCollection<T> ConcatReactive<T>(this IReactiveCollection<T> collection,
            IReactiveCollection<T> collection2)
        {
            return new ConcatCollection<T>(collection, collection2);
        }
        
        public static IReactiveCollection<T> ResizeReactive<T>(this IReactiveCollection<T> collection,
            Func<int, int> newSizeBasedOnOldSize, Func<int, T> newElemBasedOnIndex)
        {
            return new ResizeCollection<T>
            {
                collection = collection,
                newElemBasedOnIndex = newElemBasedOnIndex,
                newSizeBasedOnOldSize = newSizeBasedOnOldSize,
            };
        }

        [DebuggerDisplay("{this.ToString()}")]
        internal class ResizeCollection<T> : AbstractCollectionTransform<T>
        {
            public IReactiveCollection<T> collection;
            public Func<int, int> newSizeBasedOnOldSize;
            public Func<int, T> newElemBasedOnIndex;
            public Action<T> destroyElem;
            
            int lastRealSize;
            
            protected override IDisposable StartListenAndRefill()
            {
                var conn = collection.update.Subscribe(e =>
                {
                    if (disconected) return;
                    switch (e.type)
                    {
                        case ReactiveCollectionEventType.Reset:
                            buffer.ResetConsumeList(MakeNewCollection(e.newData));
                            break;
                        case ReactiveCollectionEventType.Insert:
                        {
                            var oldSize = buffer.Count;
                            // trying to touch collection as few times as possible due to some clusterfuck bugs in past
                            lastRealSize++;
                            var newSize = newSizeBasedOnOldSize(lastRealSize);
                            if (e.position < newSize)
                            {
                                buffer.Insert(e.position, e.newItem);
                            }
                            buffer.Resize(newSize, newElemBasedOnIndex, obj => {});
                            break;
                        }
                        case ReactiveCollectionEventType.Remove:
                        {
                            var oldSize = buffer.Count;
                            // trying to touch collection as few times as possible due to some clusterfuck bugs in past
                            lastRealSize--;
                            var newSize = newSizeBasedOnOldSize(lastRealSize);
                            if (e.position < newSize)
                            {
                                buffer.RemoveAt(e.position);
                            }
                            buffer.Resize(newSize, newElemBasedOnIndex, obj => {});
                            break;
                        }
                        case ReactiveCollectionEventType.Set:
                            if (buffer.Count > e.position)
                            {
                                buffer[e.position] = e.newItem;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
                RefillRaw();
                return conn;
            }

            SimpleList<T> MakeNewCollection(IReadOnlyList<T> current)
            {
                var collectionCount = current.Count;
                lastRealSize = collectionCount;
                int newSize = newSizeBasedOnOldSize(collectionCount);
                var coll = newSize < collectionCount ? current.Take(newSize) : current;
                var newColl = new SimpleList<T>(newSize);
                newColl.AddRange(coll);
                for (int i = collectionCount; i < newSize; i++)
                {
                    newColl.Add(newElemBasedOnIndex(i));
                }
                return newColl;
            }

            protected override void RefillRaw()
            {
                buffer.ResetConsumeList(MakeNewCollection(collection));
            }
        }
        
        [DebuggerDisplay("{this.ToString()}")]
        internal class ConcatCollection<T> : AbstractCollectionTransform<T>
        {
            readonly IReactiveCollection<T> collection;
            readonly IReactiveCollection<T> collection2;

            public ConcatCollection(IReactiveCollection<T> collection, IReactiveCollection<T> collection2)
            {
                this.collection = collection;
                this.collection2 = collection2;
            }

            int countFirst => collection.Count;

            void Process(IReactiveCollectionEvent<T> e)
            {
                if (disconected) return;

                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        buffer.RemoveRange(0, e.oldData.Count);
                        var newDataCount = e.newData.Count;
                        for (var i = 0; i < newDataCount; i++)
                        {
                            buffer.Insert(i, e.newData[i]);
                        }

                        break;
                    case ReactiveCollectionEventType.Insert:
                        buffer.Insert(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        buffer.RemoveAt(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        buffer[e.position] = e.newItem;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void Process2(IReactiveCollectionEvent<T> e)
            {
                if (disconected) return;

                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        buffer.RemoveRange(countFirst, e.oldData.Count);
                        var newDataCount = e.newData.Count;
                        for (var i = 0; i < newDataCount; i++)
                        {
                            buffer.Add(e.newData[i]);
                        }

                        break;
                    case ReactiveCollectionEventType.Insert:
                        buffer.Insert(e.position + countFirst, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        buffer.RemoveAt(e.position + countFirst);
                        break;
                    case ReactiveCollectionEventType.Set:
                        buffer[e.position + countFirst] = e.newItem;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                var disp = new MultipleDisposable();
                disp.Add(collection.update.Subscribe(Process));
                disp.Add(collection2.update.Subscribe(Process2));
                RefillBuffer();
                return disp;
            }

            protected override void RefillRaw()
            {
                buffer.Reset(collection);
                buffer.AddRange(collection2);
            }
        }
    }
}