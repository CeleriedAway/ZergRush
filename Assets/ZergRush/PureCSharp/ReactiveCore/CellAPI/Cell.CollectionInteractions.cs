using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZergRush.CodeGen;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        public static IReactiveCollection<T2> FlatMapCollection<T, T2>(this ICell<T> cell,
            Func<T, IReactiveCollection<T2>> map)
        {
            return cell.Map(v => map(v)).Join();
        }

        public static IReactiveCollection<T2> FlatMapCollectionWithDefaultOnNull<T, T2>(this ICell<T> cell,
            Func<T, IReactiveCollection<T2>> map)
        {
            return cell.FlatMapCollection(v => v != null ? map(v) : StaticCollection<T2>.Empty());
        }

        /// Creates a cell of collection from collection of cells. Useful when you need to agrigate collections of dynamic data.
        public static ICell<IEnumerable<T>> ToCellOfCollection<T>(this IEnumerable<ICell<T>> cells)
        {
            Func<IEnumerable<T>> values = () => cells.Select(cell => cell.value);
            return new AnonymousCell<IEnumerable<T>>((Action<IEnumerable<T>> reaction) =>
            {
                var group = new MultipleDisposable();
                foreach (var cell in cells)
                {
                    @group.Add(cell.OnChanged(() =>
                    {
                        if (@group.disposed) return;
                        reaction(values());
                    }));
                }

                return @group;
            }, values);
        }

        public static ICell<bool> AllTrue(this IEnumerable<ICell<bool>> cells)
        {
            return cells.ToCellOfCollection().Map(coll =>
            {
                foreach (var b in coll)
                {
                    if (!b) return false;
                }

                return true;
            });
        }

        /// If cell value is null collection has zero elements, otherwise one element equal to cell value
        public static IReactiveCollection<T> ToSingleElementCollectionEmptyWhenNull<T>(this ICell<T> cell) where T : class
        {
            return new SingleCollectionFromNullableCell<T> { cell = cell };
        }
        
        /// Collection always has one element, even when cell value is null
        public static IReactiveCollection<T> ToSingleElementCollection<T>(this ICell<T> cell)
        {
            return new SingleCollectionCell<T>{ cell = cell };
        }
        
        /// Collection has one element, when cell value is true, otherwise zero elements
        public static IReactiveCollection<T> ToSingleElementCollection<T>(this ICell<bool> cell, T element)
        {
            return new SingleElementFromBoolCell<T> { cell = cell, element = element };
        }

        /// Collection has one element equal to cell value when predicate is true, otherwise zero elements
        public static IReactiveCollection<T> ToSingleElementCollection<T>(this ICell<T> cell, Func<T, bool> predicate)
        {
            return new SingleElementFromPredicateCell<T> { cell = cell, predicate = predicate };
        }

        public static ICell<TRes> MergeCollection<T, TRes>(this IReactiveCollection<ICell<T>> cells,
            Func<IEnumerable<T>, TRes> func)
        {
            return cells.AsCell().Map(v => v.ToCellOfCollection<T>()).Join().Map(func);

            // TODO fix this impl, now it leak connections 
            //            Func<TRes> curr = () => func(cells.current.Select(cell=>cell.value));
            //            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            //            {
            //                var disp = new CellMergeMultipleDisposable<TRes>();
            //                disp.lastValue = curr();
            //                Dictionary<ICell<T>, IDisposable> connections = new Dictionary<ICell<T>, IDisposable>();
            //     leak ----> cells.BindEach(addedCell =>
            //                {
            //                    var itemConnection = addedCell.Bind(currCellVal =>
            //                    {
            //                        var currRes = curr();
            //                        if (!EqualityComparer<TRes>.Default.Equals(currRes, disp.lastValue))
            //                        {
            //                            disp.lastValue = currRes;
            //                            reaction(curr());                            
            //                        }
            //                    });
            //                    connections.Add(addedCell, itemConnection);
            //                    disp.Add(itemConnection);
            //                }, (removedCell) => {
            //                    var itemConnection = connections[removedCell];
            //                    disp.Remove(itemConnection);
            //                    itemConnection.Dispose();                    
            //                    connections.Remove(removedCell);
            //                });
            //                return disp;
            //            }, curr);
        }
        
        class SingleCollectionFromNullableCell<T> : IReactiveCollection<T> where T : class
        {
            internal ICell<T> cell;

            public IEnumerator<T> GetEnumerator()
            {
                return new CellEnumerator { cell = cell };
            }

            class CellEnumerator : IEnumerator<T>
            {
                internal ICell<T> cell;
                bool moved;

                public bool MoveNext()
                {
                    if (cell.value == null || moved) return false;
                    moved = true;
                    return true;
                }

                public void Reset()
                {
                    moved = false;
                }

                public T Current => cell.value;
                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => cell.value == null ? 0 : 1;

            public T this[int index]
            {
                get
                {
                    if (index > 0 || cell.value == null) throw new IndexOutOfRangeException();
                    return cell.value;
                }
            }

            public IEventStream<IReactiveCollectionEvent<T>> update => cell.BufferPreviousValue().Map(e =>
            {
                var vNew = e.Item1;
                var vOld = e.Item2;
                var re = new ReactiveCollectionEvent<T> { position = 0, oldItem = vOld, newItem = vNew };
                if (vNew == null) re.type = ReactiveCollectionEventType.Remove;
                else if (vOld == null) re.type = ReactiveCollectionEventType.Insert;
                else re.type = ReactiveCollectionEventType.Set;
                return re;
            });
        }
        
        class SingleElementFromBoolCell<T> : IReactiveCollection<T>
        {
            internal ICell<bool> cell;
            internal T element;

            public IEnumerator<T> GetEnumerator()
            {
                return new CellEnumerator { owner = this };
            }

            class CellEnumerator : IEnumerator<T>
            {
                internal SingleElementFromBoolCell<T> owner;
                bool moved;

                public bool MoveNext()
                {
                    if (!owner.cell.value || moved) return false;
                    moved = true;
                    return true;
                }

                public void Reset()
                {
                    moved = false;
                }

                public T Current => owner.element;
                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => cell.value ? 1 : 0;

            public T this[int index]
            {
                get
                {
                    if (index != 0 || !cell.value) throw new IndexOutOfRangeException();
                    return element;
                }
            }

            public IEventStream<IReactiveCollectionEvent<T>> update => cell.BufferPreviousValue().Map(e =>
            {
                var vNew = e.Item1;
                var vOld = e.Item2;
                var re = new ReactiveCollectionEvent<T> { position = 0 };
                if (vNew && !vOld)
                {
                    re.type = ReactiveCollectionEventType.Insert;
                    re.newItem = element;
                }
                else if (!vNew && vOld)
                {
                    re.type = ReactiveCollectionEventType.Remove;
                    re.oldItem = element;
                }
                else
                {
                    LogSink.errLog($"[SingleElementFromBoolCell] Unexpected case: vNew={vNew}, vOld={vOld}");
                    re.type = ReactiveCollectionEventType.Set;
                    re.newItem = element;
                    re.oldItem = element;
                }
                return re;
            });
        }

        class SingleElementFromPredicateCell<T> : IReactiveCollection<T>
        {
            internal ICell<T> cell;
            internal Func<T, bool> predicate;

            public IEnumerator<T> GetEnumerator()
            {
                return new CellEnumerator { owner = this };
            }

            class CellEnumerator : IEnumerator<T>
            {
                internal SingleElementFromPredicateCell<T> owner;
                bool moved;

                public bool MoveNext()
                {
                    if (moved || !owner.predicate(owner.cell.value)) return false;
                    moved = true;
                    return true;
                }

                public void Reset()
                {
                    moved = false;
                }

                public T Current => owner.cell.value;
                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => predicate(cell.value) ? 1 : 0;

            public T this[int index]
            {
                get
                {
                    if (index != 0 || !predicate(cell.value)) throw new IndexOutOfRangeException();
                    return cell.value;
                }
            }

            public IEventStream<IReactiveCollectionEvent<T>> update => cell.BufferPreviousValue()
                .Filter(e => predicate(e.Item1) || predicate(e.Item2))
                .Map(e =>
                {
                    var vNew = e.Item1;
                    var vOld = e.Item2;
                    var pNew = predicate(vNew);
                    var pOld = predicate(vOld);
                    var re = new ReactiveCollectionEvent<T> { position = 0 };
                    if (pNew && !pOld)
                    {
                        re.type = ReactiveCollectionEventType.Insert;
                        re.newItem = vNew;
                    }
                    else if (!pNew && pOld)
                    {
                        re.type = ReactiveCollectionEventType.Remove;
                        re.oldItem = vOld;
                    }
                    else
                    {
                        re.type = ReactiveCollectionEventType.Set;
                        re.newItem = vNew;
                        re.oldItem = vOld;
                    }
                    return re;
                });
        }

        class SingleCollectionCell<T> : IReactiveCollection<T>
        {
            internal ICell<T> cell;

            public IEnumerator<T> GetEnumerator()
            {
                return new CellEnumerator { cell = cell };
            }

            class CellEnumerator : IEnumerator<T>
            {
                internal ICell<T> cell;
                bool moved;

                public bool MoveNext()
                {
                    if (moved) return false;
                    moved = true;
                    return true;
                }

                public void Reset()
                {
                    moved = false;
                }

                public T Current => cell.value;
                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => 1;

            public T this[int index]
            {
                get
                {
                    if (index != 0) throw new IndexOutOfRangeException();
                    return cell.value;
                }
            }

            public IEventStream<IReactiveCollectionEvent<T>> update => cell.BufferPreviousValue().Map(e =>
            {
                var vNew = e.Item1;
                var vOld = e.Item2;
                var re = new ReactiveCollectionEvent<T> { position = 0, oldItem = vOld, newItem = vNew };
                re.type = ReactiveCollectionEventType.Set;
                return re;
            });
        }


    }
}