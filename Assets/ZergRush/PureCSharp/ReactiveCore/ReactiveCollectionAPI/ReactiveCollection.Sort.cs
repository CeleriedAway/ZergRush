using System;
using System.Diagnostics;

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
                        RefillRaw();
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
                RefillRaw();
                return collection.update.Subscribe(Process);
            }

            protected override void RefillRaw()
            {
                buffer.Clear();
                var coll = collection;
                for (int i = 0; i < coll.Count; i++)
                {
                    var item = coll[i];
                    buffer.InsertSorted(sorter, item);
                }
            }
        }
    }
}