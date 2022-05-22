using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        /// If cell value is null collection has zero elements
        public static IReactiveCollection<T> ToSingleNullableElementCollection<T>(this ICell<T> cell) where T : class
        {
            return new SingleCollectionFromNullableCell<T> { cell = cell };
        }
        
        /// Collection always has one element, even when cell value is null
        public static IReactiveCollection<T> ToSingleElementCollection<T>(this ICell<T> cell)
        {
            return new SingleCollectionCell<T>{ cell = cell };
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