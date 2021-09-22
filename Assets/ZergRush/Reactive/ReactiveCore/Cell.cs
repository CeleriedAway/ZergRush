using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace ZergRush.ReactiveCore
{
    /// <summary>
    ///     ICellReader
    ///     Represents a value that is changed over time.
    ///     In any point of time it has current value and you can always listen for its updates.
    ///     It's name comes from anologue of cells in spreadsheets, where cell's value can depend on other cells.
    /// </summary>
    public interface ICell<out T> 
    {
        IDisposable ListenUpdates(Action<T> reaction);
        T value { get; }
    }

    /// <summary>
    /// Represents a consumer of a value
    /// </summary>
    public interface ICellWriter<in T>
    {
        T value { set; }
    }
    
    /// A value that can be readed, written
    public interface IValueRW<T>
    {
        T value { get; set; }
    }

    /// A value that can be readed, observed and written
    public interface ICellRW<T> : ICell<T>, ICellWriter<T>, IValueRW<T>
    {
        new T value { get; set; }
    }
    
    public interface IConnectable
    {
        int getConnectionCount { get; }
    }

    /// <summary>
    ///     Cell presents a reactive value that is changed over time.
    ///     In any point of time it has current value and you can always listen for its updates.
    ///     It's name comes from anologue of cells in spreadsheets, where cell's value can depend on other cells.
    /// </summary>
    [Serializable, DebuggerDisplay("content: {value}")]
    public class Cell<T> : ICellRW<T>, IConnectable
    {
        //[SerializeField]
        private T val;
        [NonSerialized] protected EventStream<T> up;

        public Cell(T t)
        {
            val = t;
        }

        public Cell()
        {
        }


        public ref T valueRef => ref val;

        public T value
        {
            get { return val; }
            set
            {
                if (up != null && EqualityComparer<T>.Default.Equals(value, val) == false)
                {
                    val = value;
                    up.Send(val);
                }
                else
                {
                    val = value;
                }
            }
        }

        public IEventStream changed => updates;
        
        public EventStream<T> updates
        {
            get { return up = up ?? new EventStream<T>(); }
        }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (up == null) up = new EventStream<T>();
            return up.Subscribe(callback);
        }

        public IDisposable OnChanged(Action action)
        {
            if (up == null) up = new EventStream<T>();
            return this.up.Subscribe(_ => action());
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public void SetValue(T v)
        {
            this.value = v;
        }

        public int getConnectionCount => up == null ? 0 : up.getConnectionCount;
    }
    
    [Serializable, DebuggerDisplay("content: {value}")]
    // Normal cell but compares values as references without equals operator
    public class ReferenceEqualityCell<T> : ICellRW<T>, IConnectable where T : class
    {
        private T val;
        [NonSerialized] protected EventStream<T> up;

        public ReferenceEqualityCell(T t) { val = t; } 
        public ReferenceEqualityCell() {}

        public T value
        {
            get { return val; }
            set
            {
                if (up != null && ReferenceEquals(value, val) == false)
                {
                    val = value;
                    up.Send(val);
                }
                else
                {
                    val = value;
                }
            }
        }
        
        public EventStream<T> updates { get { return up = up ?? new EventStream<T>(); } }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (up == null) up = new EventStream<T>();
            return up.Subscribe(callback);
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public int getConnectionCount => up == null ? 0 : up.getConnectionCount;
    }

    [Serializable, DebuggerDisplay("content: {value}")]
    public class CellWithExternalUpdates<T> : Cell<T>
    {
        public void ExternalUpdate()
        {
            up.Send(value);
        }
    }

    // Does not do equation check on value assignment
    [Serializable]
    [DebuggerDisplay("{value}")]
    public sealed class UncheckedCell<T> : ICellRW<T>
    {
        //[SerializeField]
        private T val;
        [NonSerialized] private EventStream<T> up;

        public UncheckedCell(T t)
        {
            val = t;
        }

        public UncheckedCell()
        {
        }

        public T value
        {
            get { return val; }
            set
            {
                val = value;
                if (up != null) up.Send(val);
            }
        }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (up == null) up = new EventStream<T>();
            return up.Subscribe(callback);
        }
    }

    public static class StaticCell
    {
        public static ICell<bool> False = new StaticCell<bool>(false);
        public static ICell<bool> True = new StaticCell<bool>(true);
    }
    [Serializable]
    [DebuggerDisplay("{value}")]
    public sealed class StaticCell<T> : ICell<T>
    {
        public StaticCell()
        {
        }

        public StaticCell(T initial)
        {
            val = initial;
        }

        //[SerializeField]
        readonly T val = default(T);

        public T value
        {
            get { return val; }
        }

        public IDisposable ListenUpdates(Action<T> reaction)
        {
            return EmptyDisposable.value;
        }

        public IDisposable OnChanged(Action action)
        {
            return EmptyDisposable.value;
        }

        static StaticCell<T> def = new StaticCell<T>();

        public static ICell<T> Default() => def;
    }


    [DebuggerDisplay("{value}")]
    public class AnonymousCell<T> : ICell<T>
    {
        public Func<Action<T>, IDisposable> listen;
        public Func<T> current;

        public T value
        {
            get { return current(); }
        }

        public AnonymousCell(Func<Action<T>, IDisposable> subscribe, Func<T> current)
        {
            this.listen = subscribe;
            this.current = current;
        }

        public IDisposable ListenUpdates(Action<T> reaction)
        {
            return listen(reaction);
        }

        public IDisposable OnChanged(Action action)
        {
            return listen(_ => action());
        }
    }
    
    public class AnonymousRWCell<T> : AnonymousCell<T>, ICellRW<T>
    {
        readonly Action<T> sink;

        public AnonymousRWCell(Func<Action<T>, IDisposable> subscribe, Func<T> current, Action<T> sink) :
            base(subscribe, current)
        {
            this.sink = sink;
        }

        public new T value
        {
            get { return base.value; }
            set { sink(value); }
        }
    }

    public static class CollectionReactiveApi
    {
        // Calls actionConfig with current value of and subscribes to its updates with that actionConfig.
        public static IDisposable Bind<T>(this IReactiveCollection<T> list, Action<IReadOnlyList<T>> action)
        {
            action(list);
            return list.update.Subscribe(_ => action(list));
        }
        
        public static IDisposable BindCollection<T>(this IReactiveCollection<T> list, Action<IReactiveCollectionEvent<T>> action)
        {
            action(new ReactiveCollectionEvent<T>{type = ReactiveCollectionEventType.Reset, newData = list});
            return list.update.Subscribe(action);
        }
    }

    public static class CellReactiveApi
    {
        // Calls actionConfig with current value of a cell and subscribes to its updates with that actionConfig.
        [MustUseReturnValue("In most cases you should use returned value to disconnect from cell later")]
        public static IDisposable Bind<T>(this ICell<T> cell, Action<T> action)
        {
            action(cell.value);
            return cell.ListenUpdates(action);
        }
        
        public static void Bind<T>(this ICell<T> cell, IConnectionSink connectionSink, Action<T> action)
        {
            connectionSink.AddConnection(cell.ListenUpdates(action));
            action(cell.value);
        }
        
        public static void ListenUpdates<T>(this ICell<T> cell, IConnectionSink connectionSink, Action<T> action)
        {
            connectionSink.AddConnection(cell.ListenUpdates(action));
        }

        public static IEventStream<T> UpdateStream<T>(this ICell<T> cell)
        {
            if (cell is Cell<T>) return (cell as Cell<T>).updates;
            return new AnonymousEventStream<T>(cell.ListenUpdates);
        }

        [DebuggerDisplay("{value}")]
        sealed class MappedCell<T, T2> : ICell<T2>
        {
            public ICell<T> cell;
            public Func<T, T2> map;
            public IDisposable ListenUpdates(Action<T2> reaction)
            {
                var disp = new MapDisposable<T2>();
                disp.last = map(cell.value);
                disp.Disposable = cell.ListenUpdates(val =>
                {
                    var newCurr = map(val);
                    if (!EqualityComparer<T2>.Default.Equals(newCurr, disp.last))
                    {
                        disp.last = newCurr;
                        reaction(newCurr);
                    }
                });
                return disp;
            }

            public T2 value
            {
                get { return map(cell.value); }
            }
        }
        
        // Transforms type of cell with function.
        public static ICell<T2> Map<T, T2>(this ICell<T> cell, Func<T, T2> map)
        {
            if (cell == null) throw new ZergRushException($"Map cell of type {typeof(T)} is null");
            return new MappedCell<T,T2>{cell = cell, map = map};
        }
        
        public static ICell<T2> MapWithDefaultIfNull<T, T2>(this ICell<T> cell, Func<T, T2> map, T2 def = default) where T : class
        {
            return cell.Map(v => v == null ? def : map(v));
        }

//        public static void Bind<T>(this ICell<T> e, IConnectionSink connectionSink, Action<T> action)
//        {
//            connectionSink.AddConnection(e.Bind(action));
//        }

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

        // Google it for detailes. its famous function.
        public static ICell<T2> FlatMap<T, T2>(this ICell<T> cell, Func<T, ICell<T2>> map)
        {
            if (cell == null) throw new ZergRushException($"Map cell of type {typeof(T)} is null");
            return new FlatMapCell<T,T2>{cell = cell, map = map};
        }
        
        public static ICell<T2> FlatMapWithDefaultOnNull<T, T2>(this ICell<T> cell, Func<T, ICell<T2>> map) 
            where T : class
        {
            return cell.FlatMap(v => v != null ? map(v) : StaticCell<T2>.Default());
        }
        
        public static ICell<T2> FlatMapWithDefaultOnNull<T, T2>(this ICell<T> cell, Func<T, ICell<T2>> map, T2 defaultValue) 
            where T : class
        {
            return cell.FlatMap(v => v != null ? map(v) : new StaticCell<T2>(defaultValue));
        }

        public static IReactiveCollection<T2> FlatMapCollection<T, T2>(this ICell<T> cell, Func<T, IReactiveCollection<T2>> map)
        {
            return cell.Map(v => map(v)).Join();
        }

        public static IReactiveCollection<T2> FlatMapCollectionWithDefaultOnNull<T, T2>(this ICell<T> cell, Func<T, IReactiveCollection<T2>> map)
        {
            return cell.FlatMapCollection(v => v != null ? map(v) : StaticCollection<T2>.Empty());
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
                    if (group.disposed) return;
                    
                    CheckInnerCell(innerCell);

                    if (group.Second != null)
                    {
                        var innerVal = innerCell.value;
                        if (!EqualityComparer<T>.Default.Equals(group.lastValue, innerVal))
                        {
                            reaction(innerVal);
                            group.lastValue = innerVal;
                        }
                        group.Second.Dispose();
                    }

                    group.Second = innerCell.ListenUpdates(val =>
                    {
                        reaction(val);
                        group.lastValue = val;
                    });
                };

                group.lastValue = currInnerCell.value;
                func(cell.value);
                group.First = cell.ListenUpdates(func);
                return group;
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
        
        public static IEventStream<T2> FlatMap<T, T2>(this ICell<T> cell, Func<T, IEventStream<T2>> map)
        {
            return cell.Map(v => map(v)).Join();
        }
        
        public static IEventStream<T2> FlatMapWithDefaultOnNull<T, T2>(this ICell<T> cell, Func<T, IEventStream<T2>> map)
            where T : class
        {
            return cell.FlatMap(v => v != null ? map(v) : AbandonedStream<T2>.value);
        }
        
        // Creates a cell from a cell of cell.
        // It simplyfies complex data dependancies.
        // For example if you have a dynamic value inside of an object that is also dynamic.
        public static ICell<T> Join<T>(this ICell<ICell<T>> cell)
        {
            if (cell.value == null) throw new ZergRushException($"Join cell of type {typeof(T)} is null");
            return new JoinCell<T>{cell = cell};
        }

        static void CheckInnerCell(object cell)
        {
            if (cell == null)
                throw new ZergRushException("Attempt to join null inner cell");
        }

        // Makes a simple event stream in a case when event source is changed dynamicaly in time.
        public static IEventStream<T> Join<T>(this ICell<IEventStream<T>> cell)
        {
            return new AnonymousEventStream<T>(reaction =>
            {
                var group = new DoubleDisposable();
                Action<IEventStream<T>> func = (IEventStream<T> innerStream) =>
                {
                    if (group.disposed) return;
                    
                    if (@group.Second != null) @group.Second.Dispose();
                    if (innerStream != null)
                        @group.Second = innerStream.Subscribe(reaction);
                };

                @group.First = cell.Bind(func);
                return group;
            });
        }
        public static IEventStream Join(this ICell<IEventStream> cell)
        {
            return new AnonymousEventStream(reaction =>
            {
                var group = new DoubleDisposable();
                Action<IEventStream> func = (IEventStream innerStream) =>
                {
                    if (group.disposed) return;
                    if (@group.Second != null) @group.Second.Dispose();
                    if (innerStream != null)
                        @group.Second = innerStream.Subscribe(reaction);
                };

                @group.First = cell.Bind(func);
                return group;
            });
        }
        public static IDisposable OnChanged<T>(this ICell<T> cell, Action action)
        {
            return cell.ListenUpdates(_ => action());
        }

        public static ICell<bool> Is<T>(this ICell<T> cell, T value)
        {
            return cell.Map(v => EqualityComparer<T>.Default.Equals(value, v));
        }
        
        public static ICell<bool> IsNot<T>(this ICell<T> cell, T value)
        {
            return cell.Map(v => EqualityComparer<T>.Default.Equals(value, v) == false);
        }
        
        /// An experimental concept some kink of abstract lens.
        public static ICellRW<T2> MapRW<T, T2>(this ICellRW<T> cell, Func<T, T2> map, Func<T2, T> mapBack)
        {
            return new AnonymousRWCell<T2>((Action<T2> reaction) =>
            {
                var disp = new MapDisposable<T2>();
                disp.last = map(cell.value);
                disp.Disposable = cell.ListenUpdates(val =>
                {
                    var newCurr = map(val);
                    if (!EqualityComparer<T2>.Default.Equals(newCurr, disp.last))
                    {
                        disp.last = newCurr;
                        reaction(newCurr);
                    }
                });
                return disp;
            }, () => map(cell.value), v => cell.value = mapBack(v));
        }
        
        public static ICellRW<string> MapRWEnumToString<T>(this ICellRW<T> cell) 
        {
            return new AnonymousRWCell<string>((Action<string> reaction) =>
            {
                var disp = new MapDisposable<string>();
                disp.last = cell.value.ToString();
                disp.Disposable = cell.ListenUpdates(val =>
                {
                    var newCurr = val.ToString();
                    if (newCurr != disp.last)
                    {
                        disp.last = newCurr;
                        reaction(newCurr);
                    }
                });
                return disp;
            }, () => cell.value.ToString(), v => cell.value = (T) Enum.Parse(typeof(T), v));
        }

        public static ICellRW<T2> MapRWConvert<T, T2>(this ICellRW<T> cell) 
        {
            return new AnonymousRWCell<T2>((Action<T2> reaction) =>
            {
                var disp = new MapDisposable<T2>();
                disp.last = (T2) Convert.ChangeType(cell.value, typeof(T2));
                disp.Disposable = cell.ListenUpdates(val =>
                {
                    var newCurr = (T2) Convert.ChangeType(cell.value, typeof(T2));
                    if (!EqualityComparer<T2>.Default.Equals(newCurr, disp.last))
                    {
                        disp.last = newCurr;
                        reaction(newCurr);
                    }
                });
                return disp;
            }, () => (T2) Convert.ChangeType(cell.value, typeof(T2)), v => cell.value = (T) Convert.ChangeType(v, typeof(T)));
        }

        public static ICellRW<T> ReflectionFieldToRW<T>(this object obj, string fieldName)
        {
            var f = obj.GetType().GetField(fieldName);
            if (f == null) {UnityEngine.Debug.LogError($"field {fieldName} is not found in obj {obj}"); return null;}
            return obj.ReflectionFieldToRW<T>(f);
        }
        public static IValueRW<float> ToFloat(this IValueRW<int> val)
        {
            return val.MapValue(i => (float) i, f => (int) f);
        }
        public static ICellRW<T> ToCellWrapp<T>(this IValueRW<T> val)
        {
            Cell<T> c = new Cell<T>(val.value);
            c.ListenUpdates(v => val.value = v);
            return c;
        }
        public static ICellRW<T> ReflectionFieldToRW<T>(this object obj, FieldInfo f)
        {
            return new AnonymousValue<T>(v => f.SetValue(obj, v), () => (T)f.GetValue(obj)).ToCellWrapp();
        }

        public static ICellRW<string> MapToString<T>(this ICellRW<T> cell, Func<string, T> parseFunc)
        {
            return cell.MapRW(v => v.ToString(), parseFunc);
        }

        // Creates a cell of collection from collection of cells. Useful when you need to agrigate collections of dynamic data.
        public static ICell<IEnumerable<T>> ToCellOfCollection<T>(this IEnumerable<ICell<T>> cells)
        {
            Func<IEnumerable<T>> values = () => cells.Select(cell => cell.value);
            return new AnonymousCell<IEnumerable<T>>((Action<IEnumerable<T>> reaction) =>
            {
                var group = new MultipleDisposable();
                foreach (var cell in cells)
                {
                    group.Add(cell.OnChanged(() =>
                    {
                        if (group.disposed) return;
                        reaction(values());
                    }));
                }
                return group;
            }, values);
        }

        // With this function you receive previous cell value as second argument, first time its the same value.
        public static IDisposable BufferBind<T>(this ICell<T> cell, Action<T, T> action)
        {
            // Implicit lambda boxing used as a prev val storage here
            T prevVal = cell.value;
            return cell.Bind(v =>
            {
                action(v, prevVal);
                prevVal = v;
            });
        }
        

        // With this function you receive previous cell value as second argument
        public static IDisposable BufferListenUpdates<T>(this ICell<T> cell, Action<T, T> action)
        {
            // Implicit lambda boxing used as a prev val storage here
            var prevVal = cell.value;
            return cell.ListenUpdates(v =>
            {
                action(v, prevVal);
                prevVal = v;
            });
        }

        // Useful when you need previous value of a cell, it comes as a second item in the tuple.
        public static IEventStream<(T, T)> BufferPreviousValue<T>(this ICell<T> cell)
        {
            // Implicit lambda boxing used as a prev val storage here
            var prevVal = cell.value;
            return new AnonymousEventStream<(T, T)>(action =>
            {
                return cell.ListenUpdates(v =>
                {
                    action((v, prevVal));
                    prevVal = v;
                });
            });
        }

        public static IEventStream<int> Delta(this ICell<int> cell)
        {
            return cell.BufferPreviousValue().Map(i => i.Item1 - i.Item2);
        }
        public static IEventStream<float> Delta(this ICell<float> cell)
        {
            return cell.BufferPreviousValue().Map(i => i.Item1 - i.Item2);
        }

        // Creates a new cell that is updated from previous cell unless gate is closed (false),
        // if that is so it waits gate to be true, to update its value from initial cell
        public static ICell<T> Gate<T>(this ICell<T> cell, ICell<bool> gate, IConnectionSink connectionSink)
        {
            var result = new Cell<T>(cell.value);
            connectionSink.AddConnection(cell.ListenUpdates(v =>
            {
                if (gate.value) result.value = v;
            }));
            connectionSink.AddConnection(gate.ListenUpdates(v =>
            {
                if (v) result.value = cell.value;
            }));
            return result;
        }
        
        // Creates a new event that is updated from previous event unless gate is closed (false),
        // when gate opens (true), all blocked events are instantly fired 
        public static IEventStream<T> Gate<T>(this IEventStream<T> e, ICell<bool> gate, IConnectionSink connectionSink)
        {
            var events = new List<T>();
            var newE = new EventStream<T>();
            connectionSink.AddConnection(e.Subscribe(v =>
            {
                if (gate.value) newE.Send(v);
                else events.Add(v);
            }));
            connectionSink.AddConnection(gate.ListenUpdates(v =>
            {
                if (!v) return;
                foreach (var @event in events)
                {
                    newE.Send(@event);
                }
            }));
            return newE;
        }

        // Merge two dynamic values into a tuple.
        public static ICell<Tuple<T, T2>> Merge<T, T2>(this ICell<T> cell, ICell<T2> cell2)
        {
            return Merge(cell, cell2, Tuple.Create);
        }
        
        // Merge three dynamic values into a tuple.
        public static ICell<Tuple<T, T2, T3>> Merge<T, T2, T3>(this ICell<T> cell, ICell<T2> cell2, ICell<T3> cell3)
        {
            return Merge(cell, cell2, cell3, Tuple.Create);
        }

        public static ICell<Tuple<T, T2, T3, T4>> Merge<T, T2, T3, T4>(this ICell<T> cell, ICell<T2> cell2, ICell<T3> cell3, ICell<T4> cell4)
        {
            return Merge(cell, cell2, cell3, cell4, Tuple.Create);
        }

        public static ICell<Tuple<T, T2, T3, T4, T5>> Merge<T, T2, T3, T4, T5>(this ICell<T> cell, ICell<T2> cell2, ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5)
        {
            return Merge(cell, cell2, cell3, cell4, cell5, Tuple.Create);
        }

        public static ICell<Tuple<T, T2, T3, T4, T5, T6>> Merge<T, T2, T3, T4, T5, T6>(this ICell<T> cell, ICell<T2> cell2, ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5, ICell<T6> cell6) {
            return Merge(cell, cell2, cell3, cell4, cell5, cell6, Tuple.Create);
        }

        // Merge two dynamic values in new dynamic value with transformation function.
        public static ICell<TRes> Merge<T1, T2, TRes>(this ICell<T1> cell1, ICell<T2> cell2, Func<T1, T2, TRes> func)
        {
            Func<TRes> curr = () => func(cell1.value, cell2.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                return disp;
            }, curr);
        }
        public static ICell<TRes> Merge<T1, T2, T3, TRes>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3, Func<T1, T2, T3, TRes> func)
        {
            Func<TRes> curr = () => func(cell1.value, cell2.value, cell3.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                disp.Add(ListenUpdates(cell3, curr, disp, reaction));
                return disp;
            }, curr);
        }
        public static ICell<TRes> Merge<T1, T2, T3, T4, TRes>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3, ICell<T4> cell4, Func<T1, T2, T3, T4, TRes> func)
        {
            Func<TRes> curr = () => func(cell1.value, cell2.value, cell3.value, cell4.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                disp.Add(ListenUpdates(cell3, curr, disp, reaction));
                disp.Add(ListenUpdates(cell4, curr, disp, reaction));
                return disp;
            }, curr);
        }
        public static ICell<TRes> Merge<T1, T2, T3, T4, T5, TRes>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5, Func<T1, T2, T3, T4, T5, TRes> func)
        {
            Func<TRes> curr = () => func(cell1.value, cell2.value, cell3.value, cell4.value, cell5.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) =>
            {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                disp.Add(ListenUpdates(cell3, curr, disp, reaction));
                disp.Add(ListenUpdates(cell4, curr, disp, reaction));
                disp.Add(ListenUpdates(cell5, curr, disp, reaction));
                return disp;
            }, curr);
        }
        public static ICell<TRes> Merge<T1, T2, T3, T4, T5, T6, TRes>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5, ICell<T6> cell6, Func<T1, T2, T3, T4, T5, T6, TRes> func) {
            Func<TRes> curr = () => func(cell1.value, cell2.value, cell3.value, cell4.value, cell5.value, cell6.value);
            return new AnonymousCell<TRes>((Action<TRes> reaction) => {
                var disp = new CellMergeMultipleDisposable<TRes>();
                disp.lastValue = curr();
                disp.Add(ListenUpdates(cell1, curr, disp, reaction));
                disp.Add(ListenUpdates(cell2, curr, disp, reaction));
                disp.Add(ListenUpdates(cell3, curr, disp, reaction));
                disp.Add(ListenUpdates(cell4, curr, disp, reaction));
                disp.Add(ListenUpdates(cell5, curr, disp, reaction));
                disp.Add(ListenUpdates(cell6, curr, disp, reaction));
                return disp;
            }, curr);
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

        public static ICell<TRes> Merge<T, TRes>(this IReactiveCollection<ICell<T>> cells, Func<IEnumerable<T>, TRes> func)
        {
            return cells.AsCell().Map(v => v.ToCellOfCollection()).Join().Map(func);

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

        static IDisposable ListenUpdates<T, TRes>(ICell<T> cell, Func<TRes> curr, CellMergeMultipleDisposable<TRes> disp, Action<TRes> reaction)
        {
            return cell.ListenUpdates(val =>
            {
                if (disp.disposed) return;
                TRes newCurr = curr();
                if (!EqualityComparer<TRes>.Default.Equals(newCurr, disp.lastValue))
                {
                    disp.lastValue = newCurr;
                    reaction(newCurr);
                }
            });
        }


        // Unfortunately I didn't found a good way to implement Hold in anonimous cell style yet
        // If implement it in usual way then if eventStream is fired before subscribtion then its value is lost
        // So we need to subscribe right now and sink connection to lambda.
        public static ICell<T> Hold<T>(this IEventStream<T> eventStream, T initial, Action<IDisposable> connectionSink)
        {
            var cell = new Cell<T>(initial);
            connectionSink(eventStream.Subscribe(val => cell.value = val));
            return cell;
        }

        // Bind with two cells in one call
        public static IDisposable MergeBind<T, T2>(this ICell<T> cell, ICell<T2> cell2, Action<T, T2> func)
        {
            return Merge(cell, cell2, Tuple.Create).Bind(val => func(val.Item1, val.Item2));
        }

        // Bind with three cells in one call
        public static IDisposable MergeBind<T1, T2, T3>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3, Action<T1, T2, T3> func)
        {
            return Merge(cell1, cell2, cell3, Tuple.Create).Bind(val => func(val.Item1, val.Item2, val.Item3));
        }

        public static IDisposable MergeBind<T1, T2, T3, T4>(this ICell<T1> cell1, ICell<T2> cell2, ICell<T3> cell3, ICell<T4> cell4, Action<T1, T2, T3, T4> func)
        {
            return Merge(cell1, cell2, cell3, cell4, Tuple.Create).Bind(val => func(val.Item1, val.Item2, val.Item3, val.Item4));
        }
        
        public static IDisposable MergeBind<T1, T2, T3, T4, T5>(this ICell<T1> cell1, ICell<T2> cell2,
            ICell<T3> cell3, ICell<T4> cell4, ICell<T5> cell5, Action<T1, T2, T3, T4, T5> func)
        {
            return Merge(cell1, cell2, cell3, cell4, cell5, Tuple.Create)
                .Bind(val => func(val.Item1, val.Item2, val.Item3, val.Item4, val.Item5));
        }


        // Makes connection to cell and creates another cell as intermidiate buffer.
        // It can be used for optimization purposes when you need multiple connections to complex cell
        // you can materialize it to travers inner complex cell structure only once.
        public static Cell<T> Materialize<T>(this ICell<T> cell, IConnectionSink connectionSink)
        {
            var materializedCell = new Cell<T>();
            connectionSink.AddConnection(cell.Bind(val => materializedCell.value = val));
            return materializedCell;
        }
        
        // Linq support
        public static ICell<T2> Select<T, T2>(this ICell<T> cell, Func<T, T2> selector)
        {
            return Map(cell, selector);
        }

        // Linq support
        public static ICell<TR> SelectMany<T, TR>(this ICell<T> source, ICell<TR> other)
        {
            return SelectMany(source, _ => other);
        }

        // Linq support
        public static ICell<TR> SelectMany<T, TR>(this ICell<T> source, Func<T, ICell<TR>> selector)
        {
            return source.FlatMap(selector);
        }

        // Linq support
        public static ICell<TR> SelectMany<T, TC, TR>(this ICell<T> source, Func<T, ICell<TC>> collectionSelector,
            Func<T, TC, TR> resultSelector)
        {
            return source.SelectMany(x => collectionSelector(x).Select(y => resultSelector(x, y)));
        }

        public static ICell<bool> Not(this ICell<bool> value)
        {
            return value.Map(val => !val);
        }
        public static ICell<bool> And(this ICell<bool> value, ICell<bool> other)
        {
            return value.Merge(other, (b, b1) => b && b1);
        }
        public static ICell<bool> And(this ICell<bool> value, bool other)
        {
            return value.Map(b => b && other);
        }
        public static ICell<bool> Or(this ICell<bool> value, ICell<bool> other)
        {
            return value.Merge(other, (b, b1) => b || b1);
        }
        public static ICell<bool> ReactiveEquals<T>(this ICell<T> value, ICell<T> other)
        {
            return value.Merge(other, (b, b1) => EqualityComparer<T>.Default.Equals(b, b1));
        }

        // Maps cell value to object
        public static ICell<object> AsObject<T>(this ICell<T> cell)
        {
            return cell.Select(val => val as object);
        }
        
        // Cast cell type.
        public static ICell<T2> Cast<T, T2>(this ICell<T> cell) where T2 : class
        {
            return cell.Select(val => val as T2);
        }

        // Return special stream that guarantie to call listen function once filter is returned true
        // So if filter return true on initial cell value listen function will be called right now.
        public static IEventStream WhenOnce<T>(this ICell<T> cell, Func<T, bool> filter)
        {
            return new AnonymousEventStream((Action reaction) =>
            {
                if (filter(cell.value))
                {
                    reaction();
                    return EmptyDisposable.value;
                }
                else
                {
                    var disp = new SingleDisposable();
                    disp.Disposable = cell.ListenUpdates(val =>
                    {
                        if (!filter(val)) return;
                        reaction();
                        disp.Dispose();
                    });
                    return disp;
                }
            });
        }

        // Stream is called once when cell value is true
        public static IEventStream WhenOnce(this ICell<bool> cell)
        {
            return new AnonymousEventStream(reaction =>
            {
                if (cell.value)
                {
                    reaction();
                    return EmptyDisposable.value;
                }
                else
                {
                    var disp = new SingleDisposable();
                    disp.Disposable = cell.ListenUpdates(val =>
                    {
                        if (!val) return;
                        reaction();
                        disp.Dispose();
                    });
                    return disp;
                }
            });
        }


        // Result stream will be called when cell value satisfy the predicate,
        // next call will be when value changed to not satisfy predicate and then to satisfy predicate again.
        public static IEventStream When<T>(this ICell<T> cell, Func<T, bool> filter)
        {
            return new AnonymousEventStream(reaction =>
            {
                var disp = new MapDisposable<bool>();
                disp.Disposable = cell.Bind(val =>
                {
                    if (disp.last && !filter(val))
                    {
                        disp.last = false;
                    }
                    else if (!disp.last && filter(val))
                    {
                        disp.last = true;
                        reaction();
                    }
                });
                return disp;
            });
        }
        
        public static IEventStream WhenEqualsOnce<T>(this ICell<T> cell, T value)
        {
            return cell.WhenOnce(v => v.Equals(value));
        }


        // Result stream will be calles each time cell value updates and satisfy predicate.
        public static IEventStream WhenUpdatedToSatisfy<T>(this ICell<T> cell, Func<T, bool> filter)
        {
            return new AnonymousEventStream((Action reaction) =>
            {
                return cell.ListenUpdates(val =>
                {
                    if (!filter(val)) return;
                    reaction();
                });
            });
        }

        public static IEventStream When(this ICell<bool> cell)
        {
            return cell.When(i => i);
        }

        public static IEventStream WhenMoreOrEqual(this ICell<float> cell, float value)
        {
            return cell.When(v => v >= value);
        }
        
        public static IEventStream WhenMoreOrEqual(this ICell<int> cell, int value)
        {
            return cell.When(v => v >= value);
        }
        
        public static IEventStream WhenUpdatedToTrue(this ICell<bool> cell)
        {
            return cell.WhenUpdatedToSatisfy(i => i);
        }

        public static IValueRW<T2> MapValue<T1, T2>(this IValueRW<T1> val, Func<T1, T2> map, Func<T2, T1> mapBack)
        {
            return new AnonymousValue<T2>(v => val.value = mapBack(v), () => map(val.value));
        }
        
        public static IValueRW<T2> ValueCast<T1, T2>(this IValueRW<T1> val)
        {
            return new AnonymousValue<T2>(v => val.value = (T1)(object)v, () => (T2)(object)val.value);
        }
        
        public static IDisposable DoWhenTrue(this ICell<bool> condition, Func<IDisposable> disposableAction)
        {
            var disp = new DoubleDisposable();
            disp.Second = condition.Bind(v =>
            {
                if (v)
                {
                    disp.First = disposableAction();
                }
                else
                {
                    disp.First.DisconnectSafe();
                    disp.First = null;
                }
            });
            return disp;
        }

        // Value is difference bhetween current and next value.
        public static IDisposable BindDiff(this ICell<float> cell, Action<float> action)
        {
            // Implicit lambda boxing used as a prev val storage here
            float prevVal = cell.value;
            return cell.Bind(v =>
            {
                action(v - prevVal);
                prevVal = v;
            });
        }

        // Value is difference bhetween current and next value.
        public static ICell<float> Diff(this ICell<float> cell)
        {
            // Implicit lambda boxing used as a prev val storage here
            float prevVal = cell.value;
            return new AnonymousCell<float>(action =>
            {
                return cell.Bind(v =>
                {
                    action(v - prevVal);
                    prevVal = v;
                });
            }, () => cell.value - prevVal);
        }
    }

    public class AnonymousValue<T> : IValueRW<T>
    {
        Action<T> write;
        Func<T> read;

        public AnonymousValue(Action<T> write, Func<T> read)
        {
            this.write = write;
            this.read = read;
        }

        public T value
        {
            get { return read(); }
            set { write(value); }
        }
    }


    public class NullableCell<T> : Cell<T> where T : class
    {
        public NullableCell(T t) : base(t)
        {
        }

        public NullableCell()
        {
        }
    }
    
}