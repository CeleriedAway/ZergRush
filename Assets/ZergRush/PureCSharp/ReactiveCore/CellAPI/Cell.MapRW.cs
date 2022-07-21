using System;
using System.Collections.Generic;
using System.Reflection;
using ZergRush.CodeGen;

namespace ZergRush.ReactiveCore
{
    public static partial class CellReactiveApi
    {
        /// An experimental concept some kind of abstract lens.
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
            }, () => cell.value.ToString(), v => cell.value = (T)Enum.Parse(typeof(T), v));
        }

        public static ICellRW<T2> MapRWConvert<T, T2>(this ICellRW<T> cell)
        {
            return new AnonymousRWCell<T2>((Action<T2> reaction) =>
                {
                    var disp = new MapDisposable<T2>();
                    disp.last = (T2)Convert.ChangeType(cell.value, typeof(T2));
                    disp.Disposable = cell.ListenUpdates(val =>
                    {
                        var newCurr = (T2)Convert.ChangeType(cell.value, typeof(T2));
                        if (!EqualityComparer<T2>.Default.Equals(newCurr, disp.last))
                        {
                            disp.last = newCurr;
                            reaction(newCurr);
                        }
                    });
                    return disp;
                }, () => (T2)Convert.ChangeType(cell.value, typeof(T2)),
                v => cell.value = (T)Convert.ChangeType(v, typeof(T)));
        }

        public static ICellRW<string> MapCellRWToString<T>(this ICellRW<T> cell, Func<string, T> parseFunc)
        {
            return cell.MapRW(v => v.ToString(), parseFunc);
        }

        public static IValueRW<T2> MapValueRW<T1, T2>(this IValueRW<T1> val, Func<T1, T2> map, Func<T2, T1> mapBack)
        {
            return new AnonymousValue<T2>(v => val.value = mapBack(v), () => map(val.value));
        }

        public static IValueRW<T2> ValueCast<T1, T2>(this IValueRW<T1> val)
        {
            return new AnonymousValue<T2>(v => val.value = (T1)(object)v, () => (T2)(object)val.value);
        }

        public static IValueRW<float> ToFloat(this IValueRW<int> val)
        {
            return val.MapValueRW(i => (float)i, f => (int)f);
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

        public static ICellRW<T> ReflectionFieldToRW<T>(this object obj, string fieldName)
        {
            var f = obj.GetType().GetField(fieldName);
            if (f == null)
            {
                LogSink.errLog?.Invoke($"field {fieldName} is not found in obj {obj}");
                return null;
            }

            return obj.ReflectionFieldToRW<T>(f);
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
    }
}