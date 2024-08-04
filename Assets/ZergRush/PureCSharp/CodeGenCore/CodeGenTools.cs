using System;
using System.Linq;
using System.Reflection;
using System.Text;
using ZergRush;
using ZergRush.CodeGen;


public static class CodeGenTools
{
    public static T GetAttribute<T>(this Type type, bool inherit = false) where T : Attribute
    {
        var customAttributes = type.GetCustomAttributes(typeof(T), inherit);
        return customAttributes.Length > 0 ? (T)customAttributes[0] : null;
    }
    
    public static T GetAttribute<T>(this Type type, Func<T, bool> validInheritance) where T : Attribute
    {
        var t = type;
        T result = null;
        while (t != null)
        {
            var val = t.GetAttribute<T>(false);
            if (val != null && (t == type || validInheritance(val)))
            {
                result = val;
                break;
            }
            t = t.BaseType;
        }
        return result;
    }

    public static bool HasAttribute<T>(this MemberInfo field) where T : Attribute
    {
        return Attribute.IsDefined(field, typeof(T));
    }

    public static string Indent(int times)
    {
        return new String('\t', times);
    }

    public static string ToStringWithoutListLineEnd(this StringBuilder builder)
    {
        if (builder.Length > 2)
            return builder.ToString(0, builder.Length - Environment.NewLine.Length);
        return builder.ToString();
    }

    public static string ExtranctArgNames(this string args)
    {
        var argSplitted = args.Split(' ');
        var result = "";
        for (int i = 0; i < argSplitted.Length; i++)
        {
            if (i % 2 == 1)
            {
                var argsName = argSplitted[i];
                result += argsName;
            }
        }

        return result;
    }

    public static string NameWithNamespace(this Type t)
    {
        var @namespace = t.Namespace;
        if (@namespace.Valid()) return $"{@namespace}.{t.Name}";
        else return t.Name;
    }

    public static string RealName(this Type t, bool withNamespace = false)
    {
        if (t == typeof(void)) return "void";

        if (t == typeof(int)) return "int";
        if (t == typeof(uint)) return "uint";

        if(t == typeof(short)) return "short";
        if (t == typeof(ushort)) return "ushort";

        if (t == typeof(long)) return "long";
        if (t == typeof(ulong)) return "ulong";

        if (t == typeof(byte)) return "byte";
        if (t == typeof(sbyte)) return "sbyte";

        if (t == typeof(float)) return "float";

        if (t == typeof(double)) return "double";

        if (t == typeof(decimal)) return "decimal";

        if (t == typeof(string)) return "string";
        if (t == typeof(char)) return "char";

        if (t == typeof(bool)) return "bool";

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof (ValueTuple<>))
        {
            var tupleTypes = t.GetGenericArguments();
            var tupleName = $"({tupleTypes.Select(a => a.RealName(withNamespace)).PrintCollection()})";
            return tupleName;
        }

        if (Nullable.GetUnderlyingType(t) != null)
        {
            var valueToReturn = RealName(Nullable.GetUnderlyingType(t), withNamespace) + "?"; ;
            return valueToReturn;
        }

        var name = withNamespace ? t.NameWithNamespace() : t.Name;
        if (t.IsGenericParameter)
        {
            return t.Name;
        }

        if (t.IsArray)
        {
            name = t.GetElementType().RealName(withNamespace);
            name += "[]";
        }

        if (t.IsGenericType)
        {
            name = name.Substring(0, name.Length - 2);
            name += $"<{t.GetGenericArguments().Select(a => a.RealName(!a.IsGenericParameter && withNamespace)).PrintCollection()}>";
        }

        return name;
    }

    public static string MergeSig(string firstArg, string secondArg)
    {
        if (string.IsNullOrEmpty(secondArg)) return firstArg;
        if (string.IsNullOrEmpty(firstArg)) return secondArg;
        return firstArg + ", " + secondArg;
    }
}
