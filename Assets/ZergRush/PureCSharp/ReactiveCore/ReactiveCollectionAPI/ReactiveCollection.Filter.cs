using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ZergRush.ReactiveCore
{
    public static partial class ReactiveCollectionAPI
    {
        public static IReactiveCollection<T2> FilterCastReactive<T, T2>(this IReactiveCollection<T> collectione) 
            where T : class
            where T2 : class
        {
            return collectione.Filter(t => t is T2).Map(t => t as T2);
        }
        
        public static IReactiveCollection<T> Filter<T>(this IReactiveCollection<T> collection,
            Func<T, bool> predicate)
        {
            return new FilteredCollection<T>(collection, predicate);
        }

        public static IReactiveCollection<T> Filter<T>(this IReactiveCollection<T> collection,
            Func<T, ICell<bool>> predicate)
        {
            return new AdvancedFilteredCollection<T>(collection, predicate);
        }

        [DebuggerDisplay("{this.ToString()}")]
        public class AdvancedFilteredCollection<T> : AbstractCollectionTransform<T>
        {
            readonly Func<T, ICell<bool>> predicate;
            readonly IReactiveCollection<T> collection;
            Connections connetions = new Connections();
            List<int> realIndexes = new List<int>();

            public AdvancedFilteredCollection(IReactiveCollection<T> collection, Func<T, ICell<bool>> predicate)
            {
                this.collection = collection;
                this.predicate = predicate;
            }

            void Insert(int realIndex, T item)
            {
                int newIndex = 0;
                if (realIndexes.Count > 0)
                {
                    newIndex = realIndexes.UpperBound(realIndex);
                    for (var i = newIndex; i < realIndexes.Count; ++i)
                    {
                        realIndexes[i]++;
                    }
                }

                var passCell = predicate(item);
                if (passCell.value)
                {
                    var i = realIndexes.UpperBound(realIndex);
                    realIndexes.Insert(i, realIndex);
                    buffer.Insert(i, item);
                }

                connetions.Insert(realIndex, passCell.ListenUpdates(pass =>
                {
                    if (pass)
                    {
                        var realIndex = collection.IndexOf(item);
                        var i = realIndexes.UpperBound(realIndex);
                        realIndexes.Insert(i, realIndex);
                        buffer.Insert(i, item);
                    }
                    else
                    {
                        var i = buffer.IndexOf(item);
                        if (i == -1) throw new ZergRushException("this real index must be here");
                        realIndexes.RemoveAt(i);
                        buffer.RemoveAt(i);
                    }
                }));
            }

            void Remove(int realIndex)
            {
                if (realIndexes.Count == 0) return;
                var oldIndex = realIndexes.BinarySearch(realIndex);
                for (var i = oldIndex >= 0 ? oldIndex : ~oldIndex; i < realIndexes.Count; ++i)
                {
                    realIndexes[i]--;
                }

                connetions.TakeAt(realIndex).Dispose();
                if (oldIndex < 0) return;
                realIndexes.RemoveAt(oldIndex);
                buffer.RemoveAt(oldIndex);
            }

            void Process(IReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        ProperRefill();
                        break;
                    case ReactiveCollectionEventType.Insert:
                        Insert(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        Remove(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        //TODO make proper set event resolve if needed
                        Remove(e.position);
                        Insert(e.position, e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void ProperRefill()
            {
                buffer.Clear();
                connetions.DisconnectAll();
                realIndexes.Clear();

                var coll = collection;
                var i = 0;
                foreach (var item in collection)
                {
                    Insert(i, item);
                    i++;
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                var disp = new DoubleDisposable { first = connetions, second = collection.update.Subscribe(Process) };
                ProperRefill();
                return disp;
            }

            protected override void RefillRaw()
            {
                buffer.Reset(collection.Where(i => predicate(i).value));
            }
        }

        [DebuggerDisplay("{this.ToString()}")]
        public class FilteredCollection<T> : AbstractCollectionTransform<T>
        {
            readonly Func<T, bool> predicate;
            readonly IReactiveCollection<T> collection;
            List<int> realIndexes = new List<int>();

            public FilteredCollection(IReactiveCollection<T> collection, Func<T, bool> predicate)
            {
                this.collection = collection;
                this.predicate = predicate;
            }

            void Insert(int realIndex, T item)
            {
                int newIndex = 0;
                if (realIndexes.Count > 0)
                {
                    newIndex = realIndexes.UpperBound(realIndex);
                    for (var i = newIndex; i < realIndexes.Count; ++i)
                    {
                        realIndexes[i]++;
                    }
                }

                if (predicate(item) == false) return;
                realIndexes.Insert(newIndex, realIndex);
                buffer.Insert(newIndex, item);
            }

            void Remove(int realIndex)
            {
                if (realIndexes.Count == 0) return;
                var oldIndex = realIndexes.BinarySearch(realIndex);
                for (var i = oldIndex >= 0 ? oldIndex : ~oldIndex; i < realIndexes.Count; ++i)
                {
                    realIndexes[i]--;
                }

                if (oldIndex < 0) return;
                realIndexes.RemoveAt(oldIndex);
                buffer.RemoveAt(oldIndex);
            }

            void Process(IReactiveCollectionEvent<T> e)
            {
                if (disconected) return;
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        ProperRefill(e.newData);
                        break;
                    case ReactiveCollectionEventType.Insert:
                        Insert(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        Remove(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        //TODO make proper set event resolve if needed
                        Remove(e.position);
                        Insert(e.position, e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void ProperRefill(IReadOnlyList<T> list)
            {
                buffer.Clear();
                realIndexes.Clear();
                var coll = list;
                for (int i = 0; i < coll.Count; i++)
                {
                    var item = coll[i];
                    if (predicate(item))
                    {
                        realIndexes.Add(i);
                        buffer.Add(item);
                    }
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                var disp = collection.update.Subscribe(Process);
                ProperRefill(collection);
                return disp;
            }

            protected override void RefillRaw()
            {
                buffer.Reset(collection.Where(predicate));
            }
        }
    }
}