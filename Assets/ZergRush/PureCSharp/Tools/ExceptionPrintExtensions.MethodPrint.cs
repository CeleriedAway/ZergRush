using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

public static partial class ExceptionPrintExtensions
{
    public static readonly Dictionary<string, string> TypeNameAlternatives = new Dictionary<string, string>()
    {
        {
            "Single",
            "float"
        },
        {
            "Double",
            "double"
        },
        {
            "SByte",
            "sbyte"
        },
        {
            "Int16",
            "short"
        },
        {
            "Int32",
            "int"
        },
        {
            "Int64",
            "long"
        },
        {
            "Byte",
            "byte"
        },
        {
            "UInt16",
            "ushort"
        },
        {
            "UInt32",
            "uint"
        },
        {
            "UInt64",
            "ulong"
        },
        {
            "Decimal",
            "decimal"
        },
        {
            "String",
            "string"
        },
        {
            "Char",
            "char"
        },
        {
            "Boolean",
            "bool"
        },
        {
            "Single[]",
            "float[]"
        },
        {
            "Double[]",
            "double[]"
        },
        {
            "SByte[]",
            "sbyte[]"
        },
        {
            "Int16[]",
            "short[]"
        },
        {
            "Int32[]",
            "int[]"
        },
        {
            "Int64[]",
            "long[]"
        },
        {
            "Byte[]",
            "byte[]"
        },
        {
            "UInt16[]",
            "ushort[]"
        },
        {
            "UInt32[]",
            "uint[]"
        },
        {
            "UInt64[]",
            "ulong[]"
        },
        {
            "Decimal[]",
            "decimal[]"
        },
        {
            "String[]",
            "string[]"
        },
        {
            "Char[]",
            "char[]"
        },
        {
            "Boolean[]",
            "bool[]"
        }
    };

    private static string TypeNameGauntlet(this System.Type type)
    {
        string key = type.Name;
        string empty = string.Empty;
        if (TypeNameAlternatives.TryGetValue(key, out empty))
            key = empty;
        return key;
    }

    public static bool InheritsFrom<TBase>(this System.Type type) => InheritsFrom(type, typeof(TBase));

    /// <summary>
    /// Determines whether a type inherits or implements another type. Also include support for open generic base types such as List&lt;&gt;.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="baseType"></param>
    public static bool InheritsFrom(this System.Type type, System.Type baseType)
    {
        if (baseType.IsAssignableFrom(type))
            return true;
        if (type.IsInterface && !baseType.IsInterface)
            return false;
        if (baseType.IsInterface)
            return ((IEnumerable<System.Type>)type.GetInterfaces()).Contains<System.Type>(baseType);
        for (System.Type type1 = type; type1 != null; type1 = type1.BaseType)
        {
            if (type1 == baseType || baseType.IsGenericTypeDefinition && type1.IsGenericType &&
                type1.GetGenericTypeDefinition() == baseType)
                return true;
        }

        return false;
    }

    private static string CreateNiceName(System.Type type)
    {
        if (type.IsArray)
        {
            int arrayRank = type.GetArrayRank();
            return GetNiceName(type.GetElementType()) + (arrayRank == 1 ? "[]" : "[,]");
        }

        if (type.InheritsFrom(typeof(Nullable<>)))
            return GetNiceName(type.GetGenericArguments()[0]) + "?";
        if (type.IsByRef)
            return "ref " + GetNiceName(type.GetElementType());
        if (type.IsGenericParameter || !type.IsGenericType)
            return type.TypeNameGauntlet();
        StringBuilder stringBuilder = new StringBuilder();
        string name = type.Name;
        int length = name.IndexOf("`");
        if (length != -1)
            stringBuilder.Append(name.Substring(0, length));
        else
            stringBuilder.Append(name);
        stringBuilder.Append('<');
        System.Type[] genericArguments = type.GetGenericArguments();
        for (int index = 0; index < genericArguments.Length; ++index)
        {
            System.Type type1 = genericArguments[index];
            if (index != 0)
                stringBuilder.Append(", ");
            stringBuilder.Append(GetNiceName(type1));
        }

        stringBuilder.Append('>');
        return stringBuilder.ToString();
    }

    public static string GetNiceName(this System.Type type) => type.IsNested && !type.IsGenericParameter
        ? GetNiceName(type.DeclaringType) + "." + CreateNiceName(type)
        : CreateNiceName(type);

    public static bool IsExtensionMethod(this MethodBase method)
    {
        Type declaringType = method.DeclaringType;
        return declaringType.IsSealed && !declaringType.IsGenericType && !declaringType.IsNested &&
               method.IsDefined(typeof(ExtensionAttribute), false);
    }

    public static string GetFullName(this MethodBase method)
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (method.IsExtensionMethod())
            stringBuilder.Append("[ext]");
        stringBuilder.Append(method.Name);
        if (method.IsGenericMethod)
        {
            Type[] genericArguments = method.GetGenericArguments();
            stringBuilder.Append("<");
            for (int index = 0; index < genericArguments.Length; ++index)
            {
                if (index != 0)
                    stringBuilder.Append(", ");
                stringBuilder.Append(GetNiceName(genericArguments[index]));
            }

            stringBuilder.Append(">");
        }

        stringBuilder.Append("(");
        stringBuilder.Append(GetParamsNames(method));
        stringBuilder.Append(")");
        return stringBuilder.ToString();
    }

    public static string GetParamsNames(this MethodBase method)
    {
        ParameterInfo[] parameterInfoArray = method.IsExtensionMethod()
            ? ((IEnumerable<ParameterInfo>)method.GetParameters()).Skip<ParameterInfo>(1).ToArray<ParameterInfo>()
            : method.GetParameters();
        StringBuilder stringBuilder = new StringBuilder();
        int index = 0;
        for (int length = parameterInfoArray.Length; index < length; ++index)
        {
            ParameterInfo parameterInfo = parameterInfoArray[index];
            string niceName = GetNiceName(parameterInfo.ParameterType);
            stringBuilder.Append(niceName);
            stringBuilder.Append(" ");
            stringBuilder.Append(parameterInfo.Name);
            if (index < length - 1)
                stringBuilder.Append(", ");
        }

        return stringBuilder.ToString();
    }
}