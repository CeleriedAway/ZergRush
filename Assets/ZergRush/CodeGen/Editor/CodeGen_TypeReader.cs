using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZergRush.Alive;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static bool HasAttribute<T>(this Type t, bool inherit = false) where T : Attribute
        {
            return Attribute.IsDefined(t, typeof(T), inherit);
        }

        public enum ValueVrapperType
        {
            None,
            Cell,
            LivableSlot
        }

        static Mode GenMode(this Type t)
        {
            return t.IsControllable() ? Mode.PartialClass : Mode.ExtensionMethod;
        }

        // Only single attribute is supported now
        public static GenTaskCustomImpl GetCustomImplAttr(this Type t)
        {
            if (t.HasAttribute<GenIgnore>()) return null;
            //if (typeof(IZeroFormatterSegment).IsAssignableFrom(t)) return null;

            if (t.HasAttribute<GenTask>(false) == false)
            {
                var baseClass = t.BaseType;
                while (baseClass != null)
                {
                    if (baseClass.HasAttribute<GenTask>(false))
                    {
                        t = baseClass;
                        break;
                    }
                    else
                    {
                        baseClass = baseClass.BaseType;
                    }
                }
            }

            return t.GetCustomAttribute<GenTaskCustomImpl>(false);
        }

        static Dictionary<Type, GenTaskFlags> genFlagsCache = new Dictionary<Type, GenTaskFlags>();

        public static GenTaskFlags ReadGenFlags(this Type t)
        {
            if (genFlagsCache.TryGetValue(t, out var flagsCached)) return flagsCached;
            var flags = GenTaskFlags.None;
            var type = t;
            // additive traverse hierarchy for flags
            flags = type.GetCustomAttributes<GenTask>(false).Aggregate(flags, (f, task) => f | task.flags);
            while (true)
            {
                if (type.HasAttribute<GenDoNotInheritGenTags>()) break;
                var typeBaseType = type.BaseType;
                if (typeBaseType == null) break;
                type = typeBaseType;
                flags |= type.ReadGenFlags();
            }

            if (t.HasAttribute<GenIgnore>()) flags &= ~t.GetAttribute<GenIgnore>().flags;
            genFlagsCache[t] = flags;
            return flags;
        }

        public static GenTaskFlags ReadGenCustomFlags(this Type t)
        {
            var attr = t.GetCustomImplAttr();
            if (attr == null) return GenTaskFlags.None;
            return attr.flags;
        }

        static bool CanPerform(this Type t, GenTaskFlags flags)
        {
            if ((flags & GenTaskFlags.Serialization) != 0 && typeof(ISerializable).IsAssignableFrom(t))
            {
                flags ^= GenTaskFlags.Serialization;
            }

            if ((flags & GenTaskFlags.UpdateFrom) != 0 && typeof(IUpdatableFrom<>)
                .MakeGenericType(t.TypeToUpdateFrom()).IsAssignableFrom(t))
            {
                flags ^= GenTaskFlags.UpdateFrom;
            }

            if ((flags & GenTaskFlags.PooledUpdateFrom) != 0 && t.ReadGenFlags().HasFlag(GenTaskFlags.UpdateFrom))
            {
                flags ^= GenTaskFlags.PooledUpdateFrom;
            }

            if ((flags & GenTaskFlags.PooledDeserialize) != 0 && t.ReadGenFlags().HasFlag(GenTaskFlags.Deserialize))
            {
                flags ^= GenTaskFlags.PooledDeserialize;
            }

            if ((flags & GenTaskFlags.Hash) != 0 && typeof(IHashable).IsAssignableFrom(t))
            {
                flags ^= GenTaskFlags.Hash;
            }

            return flags == 0;
        }

        static GenTaskFlags ReplaceFlags(GenTaskFlags flags, GenTaskFlags from, GenTaskFlags to)
        {
            return (flags & (~from)) | to;
        }

        static GenTaskFlags DowngradeFlagsIfNeeded(Type t, GenTaskFlags flags, GenTaskFlags from, GenTaskFlags to)
        {
            if ((flags & (from | to)) == 0) return flags;
            if (t.ReadGenFlags().HasFlag(from) == false)
            {
                return ReplaceFlags(flags, from, to);
            }

            return flags;
        }

        static Dictionary<Type, List<Type>> typeRequestMap = new Dictionary<Type, List<Type>>();

        public static void RequestGen(Type t, Type requester, GenTaskFlags flags, bool allowGenericDeclRegister = false)
        {
            if (t == typeof(object)) return;
            if (t.IsPrimitive || t.IsNullable() || t.IsEnum || t.IsGenericParameter || t == typeof(string) ||
                t == typeof(byte[])) return;

            if (requester != null) typeRequestMap.TryGetOrNew(t).AddIfNotContains(requester);

            if (t.IsGenericTypeDecl() && allowGenericDeclRegister == false)
            {
                if (t.IsList() || t.IsLivableList())
                {
                }
                else
                {
                    return;
                }
            }

            GenTaskFlags registered;
            if (typeGenRequested.TryGetValue(t.NakedGenericDefinition(), out registered))
            {
                if ((flags & ~registered) == 0) return;
            }


            if (t.IsInterface) return;

            // For list cases we need downgrade flags by element type
            var flagsCheckType = t;

            if (t.IsRef()) return;

            if (t.IsCell() || t.IsLivableSlot() || t.IsList() || t.IsArray)
            {
                var typeArg = t.FirstGenericArg();
                flagsCheckType = typeArg;
                if (!typeArg.IsLoadableConfig())
                    RequestGen(typeArg, t, flags);
            }

            // Pool requests hacks
            flags = DowngradeFlagsIfNeeded(flagsCheckType, flags, GenTaskFlags.PooledUpdateFrom,
                GenTaskFlags.UpdateFrom);
            flags = DowngradeFlagsIfNeeded(flagsCheckType, flags, GenTaskFlags.PooledDeserialize,
                GenTaskFlags.Deserialize);


            if ((flags & (GenTaskFlags.Serialization | GenTaskFlags.UpdateFrom)) != 0)
            {
                if (t.IsArray == false)
                    t.CheckParameterlessConstructor(flags);
            }


            if (t.IsControllable() && t.IsGenericType && t.IsValidType())
            {
                genericInstances.TryGetOrNew(t.GetGenericTypeDefinition()).Add(t);
            }

            // For complex polymorphism cases
            if (t.IsList() == false && t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.IsControllable())
            {
                RequestGen(t.BaseType, t, flags);
            }

            if (t.IsControllable() && !t.IsLivableList())
            {
                var generationSupportFlags = t.ReadGenFlags();
                var interfaceSupportFlags = flags & ~generationSupportFlags;

                if (t.CanPerform(interfaceSupportFlags) == false)
                {
                    Error($"Type {t} can not perform {interfaceSupportFlags} with its interfaces");
                    t.CanPerform(interfaceSupportFlags); // For debug.
                    return;
                }

                flags &= ~interfaceSupportFlags;
                if (flags == 0)
                {
                    return;
                }

                RegisterPolymorph(t);
            }
            else
            {
                if (t.IsAbstract)
                {
                    Error($"Type {t} is abstract and can not be registered for flags: " + flags);
                }
            }

//            if (t.IsLivableGen() && t != typeof(Livable) && t.IsLivableContainer() == false)
//            {
//                var baseType = t.BaseType;
//                while (baseType != typeof(Livable))
//                {
//                    if (baseType.IsLivableGen() == false)
//                    {
//                        Error($"All ansestors of livable should have [GenLivable] tag, type {baseType} does not have one");
//                        return;
//                    }
//
//                    baseType = baseType.BaseType;
//                }
//            }

            if (typeof(Livable).IsAssignableFrom(t) && t.IsLivableGen() == false)
            {
                Error($"type {t} is ancestor of livable but does not have [GenLivable] tag");
            }


            if (typeGenRequested.TryGetValue(t, out registered))
            {
                var newTasks = flags & (~registered);
                if (newTasks != 0)
                {
                    typeGenRequested[t.NakedGenericDefinition()] = registered | flags;
                    tasks.Push(new GenerationTask(t, newTasks));
                }
            }
            else
            {
                typeGenRequested[t.NakedGenericDefinition()] = flags;
                tasks.Push(new GenerationTask(t, flags));
            }
        }

        static Type NakedGenericDefinition(this Type t)
        {
            if (t.IsGenericType && t.GenericTypeArguments.Any(par => par.IsGenericParameter))
                return t.GetGenericTypeDefinition();
            else return t;
        }

        static object ExtractDefaultValue(MemberInfo member)
        {
            if (member.HasAttribute<DefaultVal>())
            {
                var extractDefaultValue = member.GetCustomAttribute<DefaultVal>().val;
                return extractDefaultValue;
            }

            return null;
        }

        static Dictionary<Type, List<DataInfo>> membersForCodegenCache = new Dictionary<Type, List<DataInfo>>();

        static Dictionary<Type, List<DataInfo>>
            membersForCodegenInheretedCache = new Dictionary<Type, List<DataInfo>>();

        public static IEnumerable<DataInfo> GetMembersForCodeGen(this Type type,
            GenTaskFlags flagRestriction = GenTaskFlags.None, bool inheretedMembers = false, bool ignoreCheck = true)
        {
            IEnumerable<DataInfo> Filter(IEnumerable<DataInfo> m)
            {
                if (ignoreCheck)
                {
                    return m.Where(m => (m.ingoreFlags & flagRestriction) == 0);
                }
                else
                {
                    return m;
                }
            }

            if (ignoreCheck && inheretedMembers &&
                membersForCodegenInheretedCache.TryGetValue(type, out var resultCached))
                return Filter(resultCached);
            if (ignoreCheck && !inheretedMembers && membersForCodegenCache.TryGetValue(type, out resultCached))
                return Filter(resultCached);

            var fieldFlags = BindingFlags.Instance | BindingFlags.Public;

            if (type.IsControllable())
            {
                fieldFlags |= BindingFlags.NonPublic;
            }

            if (inheretedMembers)
            {
                fieldFlags |= BindingFlags.FlattenHierarchy;
            }
            else
            {
                fieldFlags |= BindingFlags.DeclaredOnly;
            }

            var members = new List<DataInfo>();

            foreach (var field in type.GetFields(fieldFlags))
            {
                if (field.Name.Contains("<")) continue;
                if (field.Name.EndsWith("BackingField")) continue;

                var ignore = field.HasAttribute<GenIgnore>()
                    ? field.GetCustomAttribute<GenIgnore>().flags
                    : GenTaskFlags.None;
                ignore = ignore | (field.FieldType.HasAttribute<GenIgnore>()
                    ? field.FieldType.GetCustomAttribute<GenIgnore>().flags
                    : GenTaskFlags.None);
//                ignore = ignore | (field.HasAttribute<IgnoreFormatAttribute>() ? GenTaskFlags.All : GenTaskFlags.None);

                bool nullable = field.HasAttribute<CanBeNull>();
                bool isStatic = field.HasAttribute<Immutable>();
                if (nullable && field.FieldType.IsValueType)
                    throw new Exception("CanBeNull tag on value type is invalid on field " + field);

                members.Add(new DataInfo
                {
                    type = field.FieldType, baseAccess = field.Name,
                    canBeNull = nullable, immutableData = isStatic, ingoreFlags = ignore,
                    isPrivate = field.IsPrivate, isReadOnly = field.IsInitOnly,
                    justData = field.HasAttribute<JustData>(),
                    cantBeAncestor = field.HasAttribute<CantBeAncestor>(), defaultValue = ExtractDefaultValue(field),
                    sharpMemberInfo = field
                });
            }

            // Serialize properties only if GenInclude is set;
            foreach (var property in type.GetProperties(fieldFlags))
            {
                GenTaskFlags flags;
                var forceCanBeNull = false;
                if (IsVectorElementProperty(property))
                {
                    flags = GenTaskFlags.All;
                }
                else if (property.GetCustomAttribute<GenInclude>() != null)
                {
                    flags = property.GetCustomAttribute<GenInclude>().flags;
                }
                else
                {
                    continue;
                }

                bool nullable = property.HasAttribute<CanBeNull>() || forceCanBeNull;
                bool isStatic = property.HasAttribute<Immutable>();
                if (nullable && property.PropertyType.IsValueType)
                    throw new Exception("CanBeNull tag on value type is invalid on property " + property);
                members.Add(new DataInfo
                {
                    type = property.PropertyType,
                    baseAccess = property.Name, canBeNull = nullable, immutableData = isStatic, ingoreFlags = ~flags,
                    justData = property.HasAttribute<JustData>(),
                    isPrivate = property.GetMethod.IsPrivate ||
                                (property.SetMethod != null && property.SetMethod.IsPrivate),
                    cantBeAncestor = property.HasAttribute<CantBeAncestor>(),
                    defaultValue = ExtractDefaultValue(property), sharpMemberInfo = property
                });
            }

            // Ok then we have also a procedural members in some cases
            if (type.HasAttribute<HasRefId>())
            {
                members.Add(new DataInfo { type = typeof(int), baseAccess = "Id" });
            }

            // Pastprocess members for some special cases
            foreach (var member in members)
            {
                member.realType = member.type;

                if (member.type.IsLivableContainer() && member.isReadOnly == false)
                {
                    Error($"livable container {member} in type {type} shoud be marked readonly");
                }

                member.insideConfigStorage = type.IsConfigStorage();

                if (member.type.IsLivableSlot())
                {
                    member.canBeNull = true;
                }

                // Make cell look like usual field in codegeneration process.
                var isCell = member.type.IsCell();
                if (isCell || member.type.IsLivableSlot())
                {
                    member.valueTransformer = n => n + ".value";
                    member.realType = member.type;
                    member.type = member.type.FirstGenericArg();
                    member.isValueWrapper = isCell ? ValueVrapperType.Cell : ValueVrapperType.LivableSlot;
                }
            }

            if (ignoreCheck && inheretedMembers) membersForCodegenInheretedCache[type] = members;
            if (ignoreCheck && !inheretedMembers) membersForCodegenCache[type] = members;


            // remove ignored members.
            return Filter(members);
        }

        static DataInfo PostProcessDataInfo(this DataInfo member)
        {
            member.realType = member.type;

            if (member.type.IsLivableSlot())
            {
                member.canBeNull = true;
            }

            // Make cell look like usual field in codegeneration process.
            var isCell = member.type.IsCell();
            if (isCell || member.type.IsLivableSlot())
            {
                member.valueTransformer = n => n + ".value";
                member.realType = member.type;
                member.type = member.type.FirstGenericArg();
                member.isValueWrapper = isCell ? ValueVrapperType.Cell : ValueVrapperType.LivableSlot;
            }

            return member;
        }

        static bool IsVectorElementProperty(PropertyInfo prop)
        {
            return
                prop.Name == "x" ||
                prop.Name == "y" ||
                prop.Name == "z"
                ;
        }

        public static IEnumerable<FieldInfo> ReadAllInstanceFields(this Type type)
        {
            return
                from f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                select f;
        }

        public static IEnumerable<FieldInfo> ReadPublicInstanceFields(this Type type)
        {
            return
                from f in type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                select f;
        }
    }

    public class DataInfo
    {
        public Type carrierType;
        public string name => baseAccess;
        public string baseAccess;
        public string accessPrefix;

        public string access => (!string.IsNullOrEmpty(accessPrefix) ? accessPrefix + "." : "") +
                                valueTransformer(baseAccess);

        public string realAccess => (!string.IsNullOrEmpty(accessPrefix) ? accessPrefix + "." : "") + baseAccess;
        public Type type;
        public bool canBeNull;
        public bool immutableData;
        public GenTaskFlags ingoreFlags;
        public bool isReadOnly;
        public bool sureIsNull;
        public bool isPrivate;
        public bool cantBeAncestor;
        public bool insideConfigStorage;
        public bool justData;
        public MemberInfo sharpMemberInfo;
        public Type realType; // For cases we use value wrapper transformation.

        public object defaultValue;

        public string pathName => string.IsNullOrEmpty(pathLog) ? $"\"{baseAccess}\"" : pathLog;
        public string pathLog;

        // needed for cells to access other cell with .value
        public Func<string, string> valueTransformer = n => n;

        public ZergRush.CodeGen.CodeGen.ValueVrapperType
            isValueWrapper = ZergRush.CodeGen.CodeGen.ValueVrapperType.None;

        public static DataInfo WithTypeAndName(Type t, string name)
        {
            return new DataInfo { type = t, baseAccess = name };
        }

        public override string ToString()
        {
            return $"{nameof(baseAccess)}: {baseAccess}, {nameof(type)}: {type}";
        }
    }
}