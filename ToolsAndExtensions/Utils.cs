using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ZergRush.ReactiveCore;

namespace ZergRush
{



public static partial class Utils
{
    public static string FormatEach<T>(this IEnumerable<T> self, string format)
    {
        return FormatEach(self, format, arg1 => arg1);
    }

    public static string FormatEach<T>(this IEnumerable<T> self)
    {
        return FormatEach(self, "{0}", arg1 => arg1);
    }
     
    public static string FormatEach<T>(this IEnumerable<T> self, Func<T, object> parameter)
    {
        return FormatEach(self, "{0}", parameter);
    }

    public static string FormatEach<T>(this IEnumerable<T> self, string format, params Func<T, object>[] parameters)
    {
        var builder = new StringBuilder();

        foreach (var value in self)
        {
            builder.AppendFormat(format + "\n", parameters.Select(func => func(value)).ToArray());
        }
        return builder.ToString();
    }

    public static T CastTo<T>(this string str) where T : new()
    {
        if (str.Length > 0)
        {
            if (str.Contains("%"))
            {
                str = str.Replace("%", "");
                double tempDouble = Convert.ToDouble(str) / 100.0;
                str = tempDouble.ToString();
            }

            try
            {
                return (T) Convert.ChangeType(str, typeof(T));
            }
            catch (Exception)
            {
                //Debug.LogError(e.Message + " : " + str + "; " + e.StackTrace);
                return new T();
            }
        }
        return default(T);
    }

    public static string FormatGameNumber(float number)
    {
        if (number > 10) return number.ToString("0.");
        else if (number > 1) return number.ToString("0.0");
        else return number.ToString("0.00");
    }

    public class Wrapper<T>
    {
        public T value;
    }
    
    public static Wrapper<T> Wrap<T>(T value)
    {
        return new Wrapper<T> { value = value };
    }

    public static int Loop(int i, int cycle)
    {
        int r = i % cycle;
        if (r < 0) r += cycle;
        return r; 
    }

    public static bool HasAttribute<T>(this FieldInfo field) where T : Attribute
    {
        return Attribute.IsDefined(field, typeof (T));
    }

    public static T Clone<T>(this T source) where T : class
    {
        if (!typeof(T).IsSerializable)
        {
            throw new ArgumentException("The type must be serializable.", "source");
        }

        // Don't serialize a null object, simply return the default for that object
        if (object.ReferenceEquals(source, null))
        {
            return default(T);
        }

        IFormatter formatter = new BinaryFormatter();
        System.IO.Stream stream = new MemoryStream();
        using (stream)
        {
            formatter.Serialize(stream, source);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
    }

	public static void InvokeSafe(this Action action)
	{
		if (action != null) action();
	}
    

    public static void SafeSubstract(this Cell<uint> cell, int val)
    {
        cell.value = (uint)Math.Max(0, cell.value - val);
    }
    
    public static void SafeSubstract(this Cell<float> cell, float val)
    {
        cell.value = Math.Max(0, cell.value - val);
    }
    
    public static void SafeSubstract(this Cell<ushort> cell, int val)
    {
        cell.value = (ushort)Math.Max(0, cell.value - val);
    }
    
    public static void SafeSubstract(this Cell<byte> cell, int val)
    {
        cell.value = (byte)Math.Max(0, cell.value - val);
    }
}

}
