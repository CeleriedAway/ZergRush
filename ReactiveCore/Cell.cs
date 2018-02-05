using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#else
class SerializeField : Attribute {}
#endif

namespace ZergRush.ReactiveCore
{
    /*
         Main reactive library abstraction 
         Cell<T>
         It presents a value that is changed over time.
         In any point of time it has current value and you can always listen for its updates.
         It's name comes from anologue of cells in spreadsheets, where cell's value can depend on other cells.
    */
#if NET_4_6
    public interface ICell<out T> 
#else
    public interface ICell<T> 
#endif
    {
        IDisposable ListenUpdates(Action<T> reaction);
        T value { get; }
    }

    public interface ISinkCell<T> : ICell<T>
    {
        new T value { get; set; }
    }

    [Serializable, DebuggerDisplay("{value}")]
    public class Cell<T> : ISinkCell<T>
    {
        [SerializeField] private T val;
        [NonSerialized] private EventStream<T> up;

        public Cell(T t)
        {
            val = t;
        }

        public Cell()
        {
        }

        public T value
        {
            get { return val; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(value, val) == false)
                {
                    val = value;
                    if (up != null) up.Send(val);
                }
            }
        }

        public EventStream<T> updates
        {
            get { return up = up ?? new EventStream<T>(); }
        }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (up == null) up = new EventStream<T>();
            return up.Listen(callback);
        }

        public IDisposable OnChanged(Action action)
        {
            if (up == null) up = new EventStream<T>();
            return this.up.Listen(_ => action());
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public void SetValue(T v)
        {
            this.value = v;
        }
    }

    // Does not do equation check on value assignment
    [Serializable]
    [DebuggerDisplay("{value}")]
    public sealed class UncheckedCell<T> : ISinkCell<T>
    {
        [SerializeField] private T val;
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
            return up.Listen(callback);
        }
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

        [SerializeField] readonly T val = default(T);

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

        public static StaticCell<T> Default()
        {
            return def;
        }
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
    
    public class AnonymousSinkCell<T> : AnonymousCell<T>, ISinkCell<T>
    {
        readonly Action<T> sink;

        public AnonymousSinkCell(Func<Action<T>, IDisposable> subscribe, Func<T> current, Action<T> sink) :
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

    public static class CellReactiveApi
    {
        // Calls action with current value of a cell and subscribes to its updates with that action.
        public static IDisposable Bind<T>(this ICell<T> cell, Action<T> action)
        {
            action(cell.value);
            return cell.ListenUpdates(action);
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
            return new MappedCell<T,T2>{cell = cell, map = map};
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
                    var innerCell = map(iVal);

                    if (group.second != null)
                    {
                        var innerVal = innerCell.value;
                        if (!EqualityComparer<T2>.Default.Equals(group.lastValue, innerVal))
                        {
                            reaction(innerVal);
                            group.lastValue = innerVal;
                        }
                        group.second.Dispose();
                    }

                    group.second = innerCell.ListenUpdates(val =>
                    {
                        reaction(val);
                        group.lastValue = val;
                    });
                };

                group.first = cell.Bind(func);
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
            return new FlatMapCell<T,T2>{cell = cell, map = map};
        }
        
        public static ICell<T2> FlatMapWithDefaultOnNull<T, T2>(this ICell<T> cell, Func<T, ICell<T2>> map) 
            where T : class
        {
            return cell.FlatMap(v => v != null ? map(v) : StaticCell<T2>.Default());
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
                    CheckInnerCell(innerCell);

                    if (group.second != null)
                    {
                        var innerVal = innerCell.value;
                        if (!EqualityComparer<T>.Default.Equals(group.lastValue, innerVal))
                        {
                            reaction(innerVal);
                            group.lastValue = innerVal;
                        }
                        group.second.Dispose();
                    }

                    group.second = innerCell.ListenUpdates(val =>
                    {
                        reaction(val);
                        group.lastValue = val;
                    });
                };

                group.lastValue = currInnerCell.value;
                func(cell.value);
                group.first = cell.ListenUpdates(func);
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
            if (cell.value == null) throw new JoinNullCellException();
            return new JoinCell<T>{cell = cell};
        }

        static void CheckInnerCell(object cell)
        {
            if (cell == null)
                throw new JoinNullCellException("Attempt to join null inner cell");
        }

        // Makes a simple event stream in a case when event source is changed dynamicaly in time.
        public static IEventStream<T> Join<T>(this ICell<IEventStream<T>> cell)
        {
            return new AnonymousEventStream<T>(reaction =>
            {
                var group = new DoubleDisposable();
                Action<IEventStream<T>> func = (IEventStream<T> innerStream) =>
                {
                    if (group.second != null) group.second.Dispose();
                    if (innerStream != null)
                        group.second = innerStream.Listen(reaction);
                };

                group.first = cell.Bind(func);
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

        // An experimental concept some kink of abstract lens.
        public static ISinkCell<T2> SinkMap<T, T2>(this ISinkCell<T> cell, Func<T, T2> map, Func<T2, T> mapBack)
        {
            return new AnonymousSinkCell<T2>((Action<T2> reaction) =>
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

        // Creates a cell of collection from collection of cells. Useful when you need to agrigate collections of dynamic data.
        public static ICell<IEnumerable<T>> ToCellOfCollection<T>(this IEnumerable<ICell<T>> cells)
        {
            Func<IEnumerable<T>> values = () => cells.Select(cell => cell.value);
            return new AnonymousCell<IEnumerable<T>>((Action<IEnumerable<T>> reaction) =>
            {
                var group = new ListJoinDisposable<T>();
                foreach (var cell in cells)
                {
                    group.Add(cell.OnChanged(() => reaction(values())));
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
        public static IEventStream<Tuple<T, T>> BufferPreviousValue<T>(this ICell<T> cell)
        {
            // Implicit lambda boxing used as a prev val storage here
            var prevVal = cell.value;
            return new AnonymousEventStream<Tuple<T, T>>(action =>
            {
                return cell.ListenUpdates(v =>
                {
                    action(Tuple.Create(v, prevVal));
                    prevVal = v;
                });
            });
        }

        // Merge two dynamic values into a tuple.
        public static ICell<Tuple<T, T2>> Merge<T, T2>(this ICell<T> cell, ICell<T2> cell2)
        {
            return Merge(cell, cell2, Tuple.Create);
        }

        // Merge two dynamic values in new dynamic value with transformation function.
        public static ICell<T3> Merge<T, T2, T3>(this ICell<T> cell, ICell<T2> cell2, Func<T, T2, T3> func)
        {
            Func<T3> curr = () => func(cell.value, cell2.value);
            return new AnonymousCell<T3>((Action<T3> reaction) =>
            {
                var disp = new CellJoinDisposable<T3>();
                disp.lastValue = func(cell.value, cell2.value);
                disp.first = cell.ListenUpdates(val =>
                {
                    T3 newCurr = curr();
                    if (!EqualityComparer<T3>.Default.Equals(newCurr, disp.lastValue))
                    {
                        disp.lastValue = newCurr;
                        reaction(newCurr);
                    }
                });
                disp.second = cell2.ListenUpdates(val =>
                {
                    T3 newCurr = curr();
                    if (!EqualityComparer<T3>.Default.Equals(newCurr, disp.lastValue))
                    {
                        disp.lastValue = newCurr;
                        reaction(newCurr);
                    }
                });
                return disp;
            }, curr);
        }

        // Unfortunately I didn't found a good way to implement Hold in anonimous cell style yet
        // If implement it in usual way then if eventStream is fired before subscribtion then its value is lost
        // So we need to subscribe right now and sink connection to lambda.
        public static ICell<T> Hold<T>(this IEventStream<T> eventStream, T initial, Action<IDisposable> connectionSink)
        {
            var cell = new Cell<T>(initial);
            connectionSink(eventStream.Listen(val => cell.value = val));
            return cell;
        }

        // Bind with two cells in one call
        public static IDisposable MergeBind<T, T2>(this ICell<T> cell, ICell<T2> cell2, Action<T, T2> func)
        {
            return Merge(cell, cell2, Tuple.Create).Bind(val => func(val.Item1, val.Item2));
        }


        // Makes connection to cell and creates another cell as intermidiate buffer.
        // It can be used for optimization purposes when you need multiple connections to complex cell
        // you can materialize it to travers inner complex cell structure only once.
        public static Cell<T> Materialize<T>(this ICell<T> cell, Action<IDisposable> connectionSink)
        {
            var materializedCell = new Cell<T>();
            connectionSink(cell.Bind(val => materializedCell.value = val));
            return materializedCell;
        }

        // Since cell is not covariant in old .net we need some handy overloads
#if !NET_4_6
        public static ICell<T> Join<T>(this ICell<Cell<T>> cell)
        {
            return Join(cell.Map(c => c as ICell<T>));
        }

        public static IEventStream<T> Join<T>(this ICell<EventStream<T>> cell)
        {
            return Join(cell.Map(val => val as IEventStream<T>));
        }
        
        public static ICell<IEnumerable<T>> ToCellOfCollection<T>(this Cell<T>[] cells)
        {
            return cells.Select(val => (ICell<T>) val).ToCellOfCollection();
        }

        public static ICell<IEnumerable<T>> ToCellOfCollection<T>(this IEnumerable<Cell<T>> cells)
        {
            return cells.Select(val => (ICell<T>) val).ToCellOfCollection();
        }
#endif

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
        public static ICell<bool> Or(this ICell<bool> value, ICell<bool> other)
        {
            return value.Merge(other, (b, b1) => b || b1);
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
        
        public class JoinNullCellException : System.Exception
        {
            public JoinNullCellException() : base()
            {
            }

            public JoinNullCellException(string message) : base(message)
            {
            }

            public JoinNullCellException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }
    }
    
}