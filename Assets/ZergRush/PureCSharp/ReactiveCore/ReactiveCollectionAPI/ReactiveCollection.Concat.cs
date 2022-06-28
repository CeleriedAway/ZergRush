using System;
using System.Diagnostics;

namespace ZergRush.ReactiveCore
{
    public static partial class ReactiveCollectionAPI
    {
        public static IReactiveCollection<T> ConcatReactive<T>(this IReactiveCollection<T> collection,
            IReactiveCollection<T> collection2)
        {
            return new ConcatCollection<T>(collection, collection2);
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
                RefillBuffer();
                var disp = new MultipleDisposable();
                disp.Add(collection.update.Subscribe(Process));
                disp.Add(collection2.update.Subscribe(Process2));

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