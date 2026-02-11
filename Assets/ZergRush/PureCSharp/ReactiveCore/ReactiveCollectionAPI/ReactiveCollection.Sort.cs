using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZergRush.CodeGen;

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

        public static IReactiveCollection<T> SortReactive<T>(this IReactiveCollection<T> collection,
            ICell<Func<T, T, int>> comparator)
        {
            return new AdvancedSortedCollection<T>(collection, comparator);
        }

        [DebuggerDisplay("{this.ToString()}")]
        class AdvancedSortedCollection<T> : AbstractCollectionTransform<T>
        {
            public readonly ICell<Func<T, T, int>> sorter;
            public readonly IReactiveCollection<T> collection;

            public AdvancedSortedCollection(IReactiveCollection<T> collection, ICell<Func<T, T, int>> predicate)
            {
                this.collection = collection;
                this.sorter = predicate;
            }

            void Insert(T item)
            {
                buffer.InsertSorted(sorter.value, item);
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
                var connection = sorter.ListenUpdates(_ => RefillRaw());
                var disp = new DoubleDisposable(connection, collection.update.Subscribe(Process));
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
                    buffer.InsertSorted(sorter.value, item);
                }
            }
        }

        public static IReactiveCollection<T> DistinctReactiveUnordered<T>(this IReactiveCollection<T> collection)
        {
            return new DistinctUnorderedCollection<T>(collection);
        }

        /// <summary>
        /// Sorts a reactive collection based on a reactive float key selector.
        /// When any item's key cell value changes, the item is repositioned in the sorted collection.
        /// Uses list-based storage for better performance than dictionary lookups.
        /// </summary>
        public static IReactiveCollection<T> SortReactive<T>(this IReactiveCollection<T> collection,
            Func<T, ICell<float>> keySelector)
        {
            return new SortedByKeyCellCollection<T>(collection, keySelector);
        }
        
        public static IReactiveCollection<T> SortReactive<T>(this IReactiveCollection<T> collection,
            Func<T, ICell<int>> keySelector)
        {
            return new SortedByKeyCellCollection<T>(collection, t => keySelector(t).Map(v => (float)v));
        }

        [DebuggerDisplay("{this.ToString()}")]
        class SortedByKeyCellCollection<T> : AbstractCollectionTransform<T>
        {
            public readonly Func<T, ICell<float>> keySelector;
            public readonly IReactiveCollection<T> collection;

            // Subscriptions stored by original collection index
            readonly Connections connections = new();
            // Sorted entries: (originalIndex, sortKey) - sorted by sortKey
            readonly SimpleList<(int originalIndex, float sortKey)> sortedEntries = new();

            public SortedByKeyCellCollection(IReactiveCollection<T> collection, Func<T, ICell<float>> keySelector)
            {
                this.collection = collection;
                this.keySelector = keySelector;
            }

            static int CompareEntries((int originalIndex, float sortKey) a, (int originalIndex, float sortKey) b)
            {
                return a.sortKey.CompareTo(b.sortKey);
            }

            int FindSortedEntryIndex(int originalIndex)
            {
                for (var i = 0; i < sortedEntries.Count; i++)
                {
                    if (sortedEntries[i].originalIndex == originalIndex)
                        return i;
                }
                return -1;
            }

            void InsertSortedEntry(int originalIndex, T item)
            {
                var keyCell = keySelector(item);
                var entry = (originalIndex, keyCell.value);
                
                for (var i = 0; i < sortedEntries.Count; i++)
                {
                    ref var e = ref sortedEntries.AtRef(i);
                    if (e.originalIndex >= originalIndex) e.originalIndex++;
                }
                
                var indexSorted = sortedEntries.InsertSorted(CompareEntries, entry);

                var disp = keyCell.ListenUpdates(v => Reaction(originalIndex, v));
                connections.Insert(originalIndex, disp);
                buffer.Insert(indexSorted, collection[originalIndex]);
            }

            void Remove(int originalIndex, T item)
            {
                var sortedIndex = FindSortedEntryIndex(originalIndex);
                if (sortedIndex != -1)
                {
                    sortedEntries.RemoveAt(sortedIndex);
                    buffer.RemoveAt(sortedIndex);
                }
                else
                {
                    LogSink.errLog($"Failed to find sorted entry for original index {originalIndex} during removal.");
                    return;
                }
                connections.TakeAt(originalIndex).Dispose();
                
                for (var i = 0; i < sortedEntries.Count; i++)
                {
                    ref var e = ref sortedEntries.AtRef(i);
                    if (e.originalIndex > originalIndex) e.originalIndex--;
                }
            }

            void Reaction(int originalIndex, float newValue)
            {
                // Update sort key in sorted entries
                var oldSortedIndex = FindSortedEntryIndex(originalIndex);
                var entryToMove = (originalIndex, newValue);
                sortedEntries.RemoveAt(oldSortedIndex);
                var newIndex = sortedEntries.InsertSorted(CompareEntries, entryToMove);
                if (oldSortedIndex != newIndex)
                {
                    buffer.RemoveAt(oldSortedIndex);
                    buffer.Insert(newIndex, collection[originalIndex]);
                }
            }

            void Process(IReactiveCollectionEvent<T> e)
            {
                if (disconected) return;

                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        ClearAllSubscriptions();
                        RefillRaw(e.newData, true);
                        break;
                    case ReactiveCollectionEventType.Insert:
                        InsertSortedEntry(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        Remove(e.position, e.oldItem);
                        break;
                    case ReactiveCollectionEventType.Set:
                        Remove(e.position, e.oldItem);
                        InsertSortedEntry(e.position, e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void ClearAllSubscriptions()
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    connections[i]?.Dispose();
                }
                connections.Clear();
                sortedEntries.Clear();
            }

            protected override IDisposable StartListenAndRefill()
            {
                var disp = collection.update.Subscribe(Process);
                ClearAllSubscriptions();
                RefillRaw(collection, true);
                var dispFinal = new DoubleDisposable();
                dispFinal.First = disp;
                dispFinal.Second = connections;
                return dispFinal;
            }

            protected override void RefillRaw()
            {
                RefillRaw(collection, false);
            }

            void RefillRaw(IReadOnlyList<T> data, bool connect)
            {
                sortedEntries.Clear();
                
                for (var i = 0; i < data.Count; i++)
                {
                    var t = data[i];
                    var keyCell = keySelector(t);
                    sortedEntries.InsertSorted(CompareEntries, (i, keyCell.value));
                    if (connect)
                    {
                        var origIndex = i;
                        connections.Add(keyCell.ListenUpdates(v => Reaction(origIndex, v)));
                    }
                }
                
                buffer.Reset(sortedEntries.Select(e => collection[e.originalIndex]));
            }

        }

        class DistinctUnorderedCollection<T> : AbstractCollectionTransform<T>
        {
            public readonly IReactiveCollection<T> collection;

            public DistinctUnorderedCollection(IReactiveCollection<T> collection)
            {
                this.collection = collection;
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
                        OnInsert(e);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        OnRemove(e);
                        break;
                    case ReactiveCollectionEventType.Set:
                        OnRemove(e);
                        OnInsert(e);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void OnInsert(IReactiveCollectionEvent<T> e)
            {
                var i = buffer.IndexOf(e.newItem);
                if (i == -1)
                {
                    buffer.Add(e.newItem);
                }
            }

            void OnRemove(IReactiveCollectionEvent<T> e)
            {
                var countInOriginal = collection.Count(i => EqualityComparer<T>.Default.Equals(i, e.oldItem));
                if (countInOriginal == 0)
                {
                    buffer.Remove(e.oldItem);
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
                buffer.Reset(list.Distinct());
            }

            protected override void RefillRaw()
            {
                RefillRaw(collection);
            }
        }
    }
}