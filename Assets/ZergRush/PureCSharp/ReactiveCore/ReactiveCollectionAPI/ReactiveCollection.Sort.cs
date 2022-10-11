using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ZergRush.ReactiveCore
{
    public static partial class ReactiveCollectionAPI
    {
        public static IReactiveCollection<T> SortReactive<T>(this IReactiveCollection<T> collection,
            Func<T, T, int> comparator)
        {
            return new SortedCollection<T>(collection, comparator);
        }
        
        [DebuggerDisplay("{this.ToString()}")]
        public class SortedCollection<T> : AbstractCollectionTransform<T>
        {
            public readonly Func<T, T, int> sorter;
            public readonly IReactiveCollection<T> collection;

            public SortedCollection(IReactiveCollection<T> collection, Func<T, T, int> predicate)
            {
                this.collection = collection;
                this.sorter = predicate;
            }

            void Insert(T item)
            {
                buffer.InsertSorted(sorter, item);
            }

            void Remove(T oldItem)
            {
                buffer.Remove(oldItem);
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
                        Insert(e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        Remove(e.oldItem);
                        break;
                    case ReactiveCollectionEventType.Set:
                        //TODO make proper set event resolve if needed
                        Remove(e.oldItem);
                        Insert(e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                var disp = collection.update.Subscribe(Process);
                RefillRaw();
                return disp;
            }

            protected override void RefillRaw()
            {
                if (collection is AbstractCollectionTransform<T> transformed && !transformed.connected)
                {
                    RefillRaw(collection.ToList());
                }
                else
                {
                    RefillRaw(collection);
                }
            }

            protected void RefillRaw(IReadOnlyList<T> list)
            {
                buffer.Clear();
                var coll = list;
                for (int i = 0; i < coll.Count; i++)
                {
                    var item = coll[i];
                    buffer.InsertSorted(sorter, item);
                }
            }
        }
    }
}