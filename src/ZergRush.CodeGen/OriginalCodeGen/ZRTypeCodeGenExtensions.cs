using System;
using Type = ZergRush.CodeGen.ZRType;
using System.Collections.Generic;
using System.Linq;
using ZergRush.Alive;
using ZergRush.ReactiveCore;

namespace ZergRush.CodeGen
{
    public static class ZRTypeCodeGenExtensions
    {
        public static bool HasAttribute<T>(this ZRType? type, bool inherit = false) where T : Attribute
        {
            return type.GetAttributeInfo<T>(inherit) != null;
        }

        public static bool HasAttribute<T>(this ZRMember? member) where T : Attribute
        {
            return member.GetAttributeInfo<T>() != null;
        }

        public static bool HasAttributeName(this ZRMember? member, string attributeName)
        {
            return member?.Attributes.Any(attribute => AttributeNameMatches(attribute, attributeName)) == true;
        }

        public static ZRAttributeInfo? GetAttributeInfo<T>(this ZRType? type, bool inherit = false) where T : Attribute
        {
            var current = type;
            while (current != null)
            {
                var attr = current.Attributes.FirstOrDefault(attribute => AttributeNameMatches<T>(attribute));
                if (attr != null) return attr;
                if (!inherit || (current.Options & ZRTypeOption.DoNotInheritGenTags) != 0) return null;
                current = current.BaseType;
            }

            return null;
        }

        public static ZRAttributeInfo? GetAttributeInfo<T>(this ZRMember? member) where T : Attribute
        {
            return member?.Attributes.FirstOrDefault(attribute => AttributeNameMatches<T>(attribute));
        }

        public static T? GetAttribute<T>(this ZRType? type, bool inherit = false) where T : Attribute
        {
            var info = type.GetAttributeInfo<T>(inherit);
            return info == null ? null : CreateAttribute<T>(info);
        }

        public static T? GetAttribute<T>(this ZRType? type, Func<T, bool> validInheritance) where T : Attribute
        {
            var current = type;
            while (current != null)
            {
                var attr = current.GetAttribute<T>(false);
                if (attr != null && (ReferenceEquals(current, type) || validInheritance(attr))) return attr;
                if ((current.Options & ZRTypeOption.DoNotInheritGenTags) != 0) break;
                current = current.BaseType;
            }

            return null;
        }

        public static T? GetCustomAttribute<T>(this ZRType? type, bool inherit = false) where T : Attribute
        {
            return type.GetAttribute<T>(inherit);
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this ZRType? type, bool inherit = false) where T : Attribute
        {
            var current = type;
            while (current != null)
            {
                foreach (var attr in current.Attributes.Where(AttributeNameMatches<T>))
                {
                    var created = CreateAttribute<T>(attr);
                    if (created != null) yield return created;
                }

                if (!inherit || (current.Options & ZRTypeOption.DoNotInheritGenTags) != 0) yield break;
                current = current.BaseType;
            }
        }

        public static bool IsAssignableFrom(this Type systemBaseType, ZRType? type)
        {
            if (type == null) return false;
            if (type == systemBaseType) return true;
            if (type.BaseType.IsAssignableTo(systemBaseType)) return true;
            return type.Interfaces.Any(interfaceType => interfaceType.IsAssignableTo(systemBaseType));
        }

        public static bool IsAssignableTo(this ZRType? type, Type systemBaseType)
        {
            if (type == null) return false;
            if (type == systemBaseType) return true;
            if (type.TryResolveSystemType() is { } resolved)
            {
                return systemBaseType.IsAssignableFrom(resolved);
            }

            if (type.BaseType.IsAssignableTo(systemBaseType)) return true;
            return type.Interfaces.Any(interfaceType => interfaceType.IsAssignableTo(systemBaseType));
        }

        public static bool IsControllable(this ZRType? type)
        {
            return type != null && type.ReadGenFlags() != GenTaskFlags.None;
        }

        public static bool IsList(this ZRType? type)
        {
            return type?.CommonConstruct == ZRCommonConstruct.List || type.IsReactiveCollection();
        }

        public static bool IsReactiveCollection(this ZRType? type)
        {
            return type?.Name.StartsWith("ReactiveCollection", StringComparison.Ordinal) == true;
        }

        public static bool IsDictionary(this ZRType? type)
        {
            return type?.CommonConstruct == ZRCommonConstruct.Dictionary ||
                   type?.Name.StartsWith("ConfigStorageDict", StringComparison.Ordinal) == true;
        }

        public static bool IsCollection(this ZRType? type)
        {
            return type.IsList() || type?.IsArray == true || type.IsDictionary();
        }

        public static ZRType[] CollectionElemTypes(this ZRType type)
        {
            if (type.IsList() || type.IsDictionary()) return type.GetGenericArguments();
            if (type.IsArray && type.GetElementType() != null) return new[] { type.GetElementType()! };
            throw new NotImplementedException();
        }

        public static bool IsCell(this ZRType? type)
        {
            return type?.CommonConstruct == ZRCommonConstruct.Cell || type?.BaseType.IsCell() == true;
        }

        public static ZRType FirstGenericArg(this ZRType type)
        {
            if (type.IsArray && type.GetElementType() != null) return type.GetElementType()!;
            if (type.GenericArguments.Count > 0) return type.GenericArguments[0];
            if (type.CommonConstructArgType != null) return type.CommonConstructArgType;
            if (type.BaseType != null) return type.BaseType.FirstGenericArg();
            return ZRType.FromSystemType(typeof(object));
        }

        public static ZRType SecondGenericArg(this ZRType type)
        {
            return type.GenericArguments.Count > 1 ? type.GenericArguments[1] : ZRType.FromSystemType(typeof(object));
        }

        public static bool IsString(this ZRType? type)
        {
            return type == typeof(string);
        }

        public static bool IsImmutableValueType(this ZRType? type)
        {
            return type != null && (type.IsPrimitive || type.IsEnum || type.Name == "Fix64");
        }

        public static bool IsMultipleReference(this ZRType? type)
        {
            return type != null && (type.Options & ZRTypeOption.MultipleRefs) != 0;
        }

        public static bool IsLoadableConfig(this ZRType? type)
        {
            return type.IsAssignableTo(typeof(LoadableConfig));
        }

        public static ZRType? NullableUnderlyingType(this ZRType? type)
        {
            return type?.CommonConstruct == ZRCommonConstruct.Nullable ? type.FirstGenericArg() : null;
        }

        public static bool IsImmutableData(this ZRType? type)
        {
            return type.IsImmutableType() || type.IsLoadableConfig() ||
                   (type != null && (type.Options & ZRTypeOption.Immutable) != 0);
        }

        public static bool IsImmutableType(this ZRType? type)
        {
            if (type == null) return false;
            return type.IsPrimitive || type.Name == "Fix64" || type == typeof(Guid) || type == typeof(DateTime) ||
                   type.IsEnum || type == typeof(string) ||
                   (type.IsNullable() && type.NullableUnderlyingType().IsImmutableType()) ||
                   (type.Options & ZRTypeOption.Immutable) != 0;
        }

        public static bool IsStruct(this ZRType? type)
        {
            return type?.IsValueType == true;
        }

        public static bool IsControllableStruct(this ZRType? type)
        {
            return type?.IsValueType == true && type.IsControllable();
        }

        public static bool IsGenericOfType(this ZRType? type, Type genericType)
        {
            if (type == null || !type.IsGenericType) return false;
            return type.GetGenericTypeDefinition() == genericType;
        }

        public static bool IsChildOf<T>(this ZRType? type)
        {
            return type.IsAssignableTo(typeof(T));
        }

        public static string NewInstance(this ZRType type)
        {
            if (type == typeof(string)) return "string.Empty";
            return $"new {type.RealName(true)}()";
        }

        public static IEnumerable<ZRType> Parents(this ZRType type)
        {
            var parent = type.BaseType;
            while (parent != null)
            {
                yield return parent;
                parent = parent.BaseType;
            }
        }

        public static ZRType? ParentWithTag<T>(this ZRType type) where T : Attribute
        {
            return type.Parents().FirstOrDefault(parent => parent.HasAttribute<T>());
        }

        public static T? FindTagInHierarchy<T>(this ZRType? type) where T : Attribute
        {
            return type.GetAttribute<T>(attribute => true);
        }

        public static string FileName(this ZRType type)
        {
            return type.UniqueName();
        }

        public static List<ZRType> UnknownGenericArguments(this ZRType type)
        {
            var unknownParameters = new List<ZRType>();
            ReadGenericArguments(unknownParameters, type);
            return unknownParameters;
        }

        static void ReadGenericArguments(List<ZRType> list, ZRType type)
        {
            if (!type.IsGenericType) return;
            foreach (var genericArgument in type.GetGenericArguments())
            {
                if (genericArgument.IsGenericParameter) list.Add(genericArgument);
                else ReadGenericArguments(list, genericArgument);
            }
        }

        public static string GenericParametersSuffix(this ZRType type)
        {
            var unknownParameters = type.UnknownGenericArguments();
            return unknownParameters.Count == 0 ? "" : $"<{unknownParameters.ToCodeGenListString()}>";
        }

        public static ZRType EnrichGeneric(this ZRType type, ZRType genType)
        {
            if (!type.IsGenericType || type.IsConstructedGenericType) return type;
            var clone = new ZRType
            {
                Name = type.Name,
                Namespace = type.Namespace,
                FullName = type.FullName,
                MetadataName = type.MetadataName,
                WrittenName = type.WrittenName,
                Kind = type.Kind,
                CommonConstruct = type.CommonConstruct,
                GenericDefinition = type,
                GenericArguments = { genType },
                CommonConstructArgType = genType,
                Options = type.Options | ZRTypeOption.ConstructedGeneric
            };
            return clone;
        }

        public static string GenericParametersConstraints(this ZRType type)
        {
            if (!type.IsGenericType) return "";
            return string.Join(Environment.NewLine, type.UnknownGenericArguments()
                .Select(parameter =>
                {
                    var parameterInfo = parameter.GenericParameters.FirstOrDefault();
                    var constraints = parameterInfo?.Constraints ?? new List<ZRType>();
                    var printed = constraints.Select(c => c.RealName(true)).Distinct().ToCodeGenListString();
                    if ((parameterInfo?.Attributes & System.Reflection.GenericParameterAttributes.DefaultConstructorConstraint) != 0)
                    {
                        printed += ", new()";
                    }

                    return string.IsNullOrEmpty(printed) ? "" : $"where {parameter.Name} : {printed}";
                }));
        }

        public static string UniqueName(this ZRType type, bool withNamespace = true)
        {
            if (type.IsNullable())
            {
                return type.NullableUnderlyingType()!.UniqueName(withNamespace) + "Nullable";
            }

            var name = withNamespace && type.Namespace.Valid()
                ? $"{type.Namespace.Replace('.', '_')}_{type.Name}"
                : type.Name;

            if (type.IsArray)
            {
                return type.GetElementType()!.UniqueName(withNamespace) + "_Array";
            }

            if (type.IsGenericType)
            {
                name = name.Contains('`') ? name[..name.IndexOf('`')] : name;
                name += $"_{type.GetGenericArguments().Select(a => a.UniqueName(!a.IsGenericParameter && withNamespace)).ToCodeGenListString("_")}";
            }

            return SanitizeTypeName(name);
        }

        public static string ClearName(this ZRType type)
        {
            return type.Name.Contains('`') ? type.Name[..type.Name.IndexOf('`')] : type.Name;
        }

        public static string NameWithNamespace(this ZRType type)
        {
            return type.Namespace.Valid() ? $"{type.Namespace}.{type.Name}" : type.Name;
        }

        public static string RealName(this ZRType? type, bool withNamespace = false)
        {
            if (type == null) return "object";
            if (type.Kind == ZRTypeKind.Void) return "void";
            if (type == typeof(int)) return "int";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(short)) return "short";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(string)) return "string";
            if (type == typeof(char)) return "char";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(object)) return "object";

            if (type.IsTuple())
            {
                return $"({type.GetGenericArguments().Select(a => a.RealName(withNamespace)).ToCodeGenListString()})";
            }

            if (type.IsNullable())
            {
                return type.NullableUnderlyingType().RealName(withNamespace) + "?";
            }

            var name = withNamespace ? type.NameWithNamespace() : type.Name;
            if (type.IsGenericParameter) return type.Name;
            if (type.IsArray) return type.GetElementType().RealName(withNamespace) + "[]";
            if (type.IsGenericType)
            {
                name = name.Contains('`') ? name[..name.IndexOf('`')] : name;
                name += $"<{type.GetGenericArguments().Select(a => a.RealName(!a.IsGenericParameter && withNamespace)).ToCodeGenListString()}>";
            }

            return name;
        }

        public static GenTaskCustomImpl? ToAttribute(this ZRCustomImplInfo info)
        {
            return new GenTaskCustomImpl(info.Flags, info.GenerateBaseMethods, info.Inheritable);
        }

        static string SanitizeTypeName(string name)
        {
            return name.Replace(".", "_")
                .Replace("<", "_")
                .Replace(">", "")
                .Replace(",", "_")
                .Replace(" ", "")
                .Replace("[]", "_Array");
        }

        static bool AttributeNameMatches<T>(ZRAttributeInfo attribute) where T : Attribute
        {
            return AttributeNameMatches(attribute, typeof(T).Name);
        }

        static bool AttributeNameMatches(ZRAttributeInfo attribute, string name)
        {
            var normalized = name.EndsWith("Attribute", StringComparison.Ordinal)
                ? name[..^"Attribute".Length]
                : name;
            return string.Equals(attribute.Name, normalized, StringComparison.Ordinal) ||
                   string.Equals(attribute.FullName, name, StringComparison.Ordinal) ||
                   attribute.FullName.EndsWith("." + normalized, StringComparison.Ordinal);
        }

        static T? CreateAttribute<T>(ZRAttributeInfo info) where T : Attribute
        {
            if (typeof(T) == typeof(GenTask))
                return (T)(Attribute)new GenTask(ArgAt(info, 0, GenTaskFlags.None));
            if (typeof(T) == typeof(GenTaskCustomImpl))
                return (T)(Attribute)new GenTaskCustomImpl(
                    ArgAt(info, 0, GenTaskFlags.None),
                    ArgAt(info, 1, false),
                    ArgAt(info, 2, true));
            if (typeof(T) == typeof(GenIgnore))
                return (T)(Attribute)new GenIgnore(ArgAt(info, 0, GenTaskFlags.All));
            if (typeof(T) == typeof(GenInclude))
                return (T)(Attribute)new GenInclude(ArgAt(info, 0, GenTaskFlags.All));
            if (typeof(T) == typeof(GenTargetFolder))
                return (T)(Attribute)new GenTargetFolder(
                    ArgAt(info, 0, (string?)null),
                    ArgAt(info, 1, true),
                    ArgAt(info, 2, 1));
            if (typeof(T) == typeof(DefaultVal))
                return (T)(Attribute)new DefaultVal(ArgAt<object?>(info, 0, null));
            if (typeof(T) == typeof(GenArrayLengthConstraint))
                return (T)(Attribute)new GenArrayLengthConstraint(ArgAt(info, 0, 100000));

            return Activator.CreateInstance(typeof(T)) as T;
        }

        static T ArgAt<T>(ZRAttributeInfo info, int index, T fallback)
        {
            if (index >= info.ConstructorArguments.Count) return fallback;
            var value = info.ConstructorArguments[index];
            if (value is T typed) return typed;
            if (typeof(T).IsEnum && value is int intValue) return (T)Enum.ToObject(typeof(T), intValue);
            if (typeof(T).IsEnum && value is string stringValue && Enum.TryParse(typeof(T), stringValue, out var enumValue))
                return (T)enumValue;
            return fallback;
        }
    }
}
