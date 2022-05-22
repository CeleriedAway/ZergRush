using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        /// Creates a cell from a cell of cell.
        /// It simplifies complex data dependencies.
        /// For example if you have a dynamic value inside of an object that is also dynamic.
        public static ICell<T> Join<T>(this ICell<ICell<T>> cell)
        {
            if (cell.value == null) throw new ZergRushException($"Join cell of type {typeof(T)} is null");
            return new JoinCell<T> { cell = cell };
        }

        /// Makes a simple event stream in a case when event source is changed dynamicaly in time.
        public static IEventStream<T> Join<T>(this ICell<IEventStream<T>> cell)
        {
            return new AnonymousEventStream<T>(reaction =>
            {
                var group = new DoubleDisposable();
                Action<IEventStream<T>> func = (IEventStream<T> innerStream) =>
                {
                    if (@group.disposed) return;

                    if (@group.Second != null) @group.Second.Dispose();
                    if (innerStream != null)
                        @group.Second = innerStream.Subscribe((Action<T>)reaction);
                };

                @group.First = cell.Bind(func);
                return @group;
            });
        }

        public static IEventStream Join(this ICell<IEventStream> cell)
        {
            return new AnonymousEventStream(reaction =>
            {
                var group = new DoubleDisposable();
                Action<IEventStream> func = (IEventStream innerStream) =>
                {
                    if (@group.disposed) return;
                    if (@group.Second != null) @group.Second.Dispose();
                    if (innerStream != null)
                        @group.Second = innerStream.Subscribe(reaction);
                };

                @group.First = cell.Bind(func);
                return @group;
            });
        }
        
        /// Join and Map in one function
        public static ICell<T2> FlatMap<T, T2>(this ICell<T> cell, Func<T, ICell<T2>> map)
        {
            if (cell == null) throw new ZergRushException($"Map cell of type {typeof(T)} is null");
            return new FlatMapCell<T, T2> { cell = cell, map = map };
        }

        public static ICell<T2> FlatMapWithDefaultOnNull<T, T2>(this ICell<T> cell, Func<T, ICell<T2>> map)
            where T : class
        {
            return cell.FlatMap(v => v != null ? map(v) : StaticCell<T2>.Default());
        }

        public static ICell<T2> FlatMapWithDefaultOnNull<T, T2>(this ICell<T> cell, Func<T, ICell<T2>> map,
            T2 defaultValue)
            where T : class
        {
            return cell.FlatMap(v => v != null ? map(v) : new StaticCell<T2>(defaultValue));
        }

        public static IEventStream<T2> FlatMap<T, T2>(this ICell<T> cell, Func<T, IEventStream<T2>> map)
        {
            return cell.Map(v => map(v)).Join();
        }

        public static IEventStream<T2> FlatMapWithDefaultOnNull<T, T2>(this ICell<T> cell,
            Func<T, IEventStream<T2>> map)
            where T : class
        {
            return cell.FlatMap(v => v != null ? map(v) : AbandonedStream<T2>.value);
        }
        
        [DebuggerDisplay("{value}")]
        sealed class FlatMapCell<T, T2> : ICell<T2>
        {
            public ICell<T> cell;
            public Func<T, ICell<T2>> map;

            public IDisposable ListenUpdates(Action<T2> reaction)
            {
                var group = new CellJoinDisposable<T2>();
                group.lastValue = value;

                Action<T> func = iVal =>
                {
                    if (group.disposed) return;
                    var innerCell = map(iVal);
                    if (@group.Second != null)
                    {
                        var innerVal = innerCell.value;
                        if (!EqualityComparer<T2>.Default.Equals(group.lastValue, innerVal))
                        {
                            reaction(innerVal);
                            group.lastValue = innerVal;
                        }

                        @group.Second.Dispose();
                    }

                    @group.Second = innerCell.ListenUpdates(val =>
                    {
                        reaction(val);
                        group.lastValue = val;
                    });
                };

                @group.First = cell.Bind(func);
                return group;
            }

            public T2 value
            {
                get { return map(cell.value).value; }
            }
        }

        
        [DebuggerDisplay("{value}")]
        sealed class JoinCell<T> : ICell<T>
        {
            public ICell<ICell<T>> cell;
            public IDisposable ListenUpdates(Action<T> reaction)
            {
                ICell<T> currInnerCell = cell.value;
                CheckInnerCell(currInnerCell);

                var group = new CellJoinDisposable<T>();

                Action<ICell<T>> func = innerCell =>
                {
                    if (@group.disposed) return;

                    CheckInnerCell(innerCell);

                    if (@group.Second != null)
                    {
                        var innerVal = innerCell.value;
                        if (!EqualityComparer<T>.Default.Equals(@group.lastValue, innerVal))
                        {
                            reaction(innerVal);
                            @group.lastValue = innerVal;
                        }

                        @group.Second.Dispose();
                    }

                    @group.Second = innerCell.ListenUpdates(val =>
                    {
                        reaction(val);
                        @group.lastValue = val;
                    });
                };

                @group.lastValue = currInnerCell.value;
                func(cell.value);
                @group.First = cell.ListenUpdates(func);
                return @group;
            }

            public T value
            {
                get
                {
                    var innerCell = cell.value;
                    CheckInnerCell(innerCell);
                    return innerCell.value;
                }
            }
        }
        
        static void CheckInnerCell(object cell)
        {
            if (cell == null)
                throw new ZergRushException("Attempt to join null inner cell");
        }
    }
}