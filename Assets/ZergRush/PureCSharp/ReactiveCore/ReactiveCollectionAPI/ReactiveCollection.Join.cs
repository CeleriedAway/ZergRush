using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZergRush.CodeGen;

namespace ZergRush.ReactiveCore
{
    public static partial class ReactiveCollectionAPI
    {
        public static IReactiveCollection<T> Join<T>(this ICell<IReactiveCollection<T>> cellOfCollection)
        {
            // this implementation does simple reset
            return new JoinCellOfCollection<T> { cellOfCollection = cellOfCollection };
        }

        public static IReactiveCollection<T> JoinIfUniqueElements<T>(this ICell<IReactiveCollection<T>> cellOfCollection)
        {
            // this implementation tries to find out what was different between collections and update accordingly
            // but does not work well if elements are not unique
            return new ReactiveCollectionFromCellOfCollection<T> { cell = cellOfCollection };
        }

        public static IReactiveCollection<T> Join<T>(
            this IReactiveCollection<IReactiveCollection<T>> collectionOfCollection)
        {
            return new JoinCollectionOfCollection<T> { collection = collectionOfCollection };
        }
        
        public static IReactiveCollection<T> Join<T>(this IReactiveCollection<ICell<T>> coll)
        {
            return new ReactiveCollectionCellJoin<T> { coll = coll };
        }
        
        public static IEventStream<T> JoinStreams<T>(this IReactiveCollection<IEventStream<T>> collection)
        {
            return new AnonymousEventStream<T>(action =>
            {
                var connections = new Connections();
                var disposable = new DoubleDisposable
                {
                    First = connections,
                };
                disposable.Second =
                    /// TODO It can be done more effectively then asCell call but much more complex
                    collection.AsCell().Bind(coll =>
                    {
                        connections.DisconnectAll();
                        if (disposable.disposed) return;
                        connections.AddRange(coll.Select<IEventStream<T>, IDisposable>(item => item.Subscribe(action)));
                    });
                return disposable;
            });
        }


        class JoinCollectionOfCollection<T> : AbstractCollectionTransform<T>
        {
            public IReactiveCollection<IReactiveCollection<T>> collection;
            public Connections collectionConnections = new Connections();

            protected override IDisposable StartListenAndRefill()
            {
                //Debug.Log("Start listen and refill");
                void OnRemove(IReactiveCollectionEvent<IReactiveCollection<T>> reactiveCollectionEvent1)
                {
                    //Debug.Log($"remove {reactiveCollectionEvent1.position} {reactiveCollectionEvent1.type} {id}");
                    collectionConnections.RemoveAndDisposeConnectionAt(reactiveCollectionEvent1.position);
                    var removeStartIndex = FinalStartIndex(reactiveCollectionEvent1.position);
                    for (int i = 0; i < reactiveCollectionEvent1.oldItem.Count; i++)
                    {
                        buffer.RemoveAt(removeStartIndex);
                    }
                }

                void OnInsert(IReactiveCollectionEvent<IReactiveCollection<T>> reactiveCollectionEvent)
                {
                    var index = FinalStartIndex(reactiveCollectionEvent.position);
                    var newItemCount = reactiveCollectionEvent.newItem.Count;
                    for (int i = 0; i < newItemCount; i++)
                    {
                        buffer.Insert(i + index, reactiveCollectionEvent.newItem[i]);
                    }

                    SubscribeCollection(reactiveCollectionEvent.newItem, reactiveCollectionEvent.position);
                }
                
                DoubleDisposable allConnections = new DoubleDisposable();
                allConnections.First = collectionConnections;
                allConnections.Second = collection.BindCollection(e =>
                {
                    switch (e.type)
                    {
                        case ReactiveCollectionEventType.Reset:
                            //Debug.Log($"Reset event to count {e.newData.Count} {id}");
                            RefillRaw(e.newData);
                            collectionConnections.DisconnectAll();
                            for (int i = 0; i < e.newData.Count; i++)
                            {
                                SubscribeCollection(e.newData[i], i);
                            }

                            break;
                        case ReactiveCollectionEventType.Insert:
                            OnInsert(e);
                            break;
                        case ReactiveCollectionEventType.Remove:
                            OnRemove(e);
                            break;
                        case ReactiveCollectionEventType.Set:
                            //Debug.Log($"set event to count {e.position} {e.newData}");
                            OnRemove(e);
                            OnInsert(e);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
                return allConnections;
            }

            int FinalStartIndex(int collectionIndex)
            {
                var index = 0;
                for (int j = 0; j < collectionIndex; j++)
                {
                    index += collection[j].Count;
                }

                return index;
            }

            void SubscribeCollection(IReactiveCollection<T> coll, int index)
            {
                var disWrap = new SingleDisposable();
                disWrap.Disposable = coll.update.Subscribe(e =>
                {
                    int innerStartIndex = FinalStartIndex(collectionConnections.IndexOf(disWrap));
                    switch (e.type)
                    {
                        case ReactiveCollectionEventType.Reset:
                            for (int i = 0; i < e.oldData.Count; i++)
                            {
                                buffer.RemoveAt(innerStartIndex);
                            }

                            for (int i = 0; i < e.newData.Count; i++)
                            {
                                buffer.Insert(i + innerStartIndex, e.newData[i]);
                            }

                            break;
                        case ReactiveCollectionEventType.Insert:
                            buffer.Insert(innerStartIndex + e.position, e.newItem);
                            break;
                        case ReactiveCollectionEventType.Remove:
                            buffer.RemoveAt(innerStartIndex + e.position);
                            break;
                        case ReactiveCollectionEventType.Set:
                            buffer[innerStartIndex + e.position] = e.newItem;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });

                collectionConnections.Insert(index, disWrap);
            }

            protected void RefillRaw(IReadOnlyList<IReactiveCollection<T>> list)
            {
                buffer.Reset(list.SelectMany(c => c));
            }
            protected override void RefillRaw()
            {
                RefillRaw(collection);
            }
        }
        
        class ReactiveCollectionFromCellOfCollection<T> : AbstractCollectionTransform<T>
        {
            public ICell<IReactiveCollection<T>> cell;

            protected override IDisposable StartListenAndRefill()
            {
                var disp = cell.ListenUpdates(coll =>
                {
                    if (coll == null)
                    {
                        buffer.Reset();
                        return;
                    }

                    var newItems = coll as T[] ?? coll.ToArray<T>();
                    for (var index = 0; index < newItems.Length; index++)
                    {
                        var item = newItems[index];
                        if (buffer.Contains(item)) continue;
                        buffer.Add(item);
                    }

                    for (var index = buffer.Count - 1; index >= 0; index--)
                    {
                        var oldItem = buffer[index];
                        if (newItems.Contains(oldItem)) continue;
                        buffer.RemoveAt(index);
                    }
                });
                RefillRaw();
                return disp;
            }

            protected override void RefillRaw()
            {
                buffer.Reset(cell.value);
            }
        }

        class JoinCellOfCollection<T> : IReactiveCollection<T>
        {
            public ICell<IReactiveCollection<T>> cellOfCollection;

            public IEnumerator<T> GetEnumerator()
            {
                return cellOfCollection.value.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEventStream<IReactiveCollectionEvent<T>> update
            {
                get
                {
                    var cellUpdates = cellOfCollection.BufferPreviousValue().Map(tuple => new ReactiveCollectionEvent<T>
                    {
                        type = ReactiveCollectionEventType.Reset,
                        newData = tuple.Item1,
                        oldData = tuple.Item2
                    });
                    return cellOfCollection.Map(coll => coll.update).Join().MergeWith(cellUpdates);
                }
            }

            public int Count => cellOfCollection.value.Count;

            public T this[int index] => cellOfCollection.value[index];
        }

        class ReactiveCollectionCellJoin<T> : AbstractCollectionTransform<T>
        {
            public IReactiveCollection<ICell<T>> coll;

            Connections connetions = new Connections();

            void Insert(int realIndex, ICell<T> item)
            {
                buffer.Insert(realIndex, item.value);
                connetions.Insert(realIndex, item.ListenUpdates(val => UpdateCell(item, val)));
            }

            void Remove(int realIndex)
            {
                connetions.TakeAt(realIndex).Dispose();
                buffer.RemoveAt(realIndex);
            }

            void Set(int realIndex, ICell<T> item)
            {
                connetions[realIndex].Dispose();
                connetions[realIndex] = item.ListenUpdates(val => UpdateCell(item, val));
                buffer[realIndex] = item.value;
            }

            void UpdateCell(ICell<T> cell, T val)
            {
                var indexOf = coll.IndexOf(cell);
                if (indexOf == -1)
                {
                    LogSink.errLog?.Invoke($"Collection of cells join error");
                    return;
                }
                buffer[indexOf] = val;
            }

            void ProperRefill(IReadOnlyList<ICell<T>> newList)
            {
                connetions.DisconnectAll();
                for (int i = 0; i < newList.Count; i++)
                {
                    var cell = newList[i];
                    connetions.Add(cell.ListenUpdates(val => UpdateCell(cell, val)));
                }

                buffer.Reset(newList.Select(l => l.value));
            }

            void Process(IReactiveCollectionEvent<ICell<T>> e)
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
                        Set(e.position, e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListenAndRefill()
            {
                var disp = new DoubleDisposable
                {
                    First = connetions,
                    Second = coll.update.Subscribe(Process)
                };
                ProperRefill(coll);
                return disp;
            }

            protected override void RefillRaw()
            {
                buffer.Clear();
                foreach (var cell in coll)
                {
                    buffer.Add(cell.value);
                }
            }
        }
    }
}