using System;
using Type = ZergRush.CodeGen.ZRType;
using System.Collections.Generic;
using System.Linq;
using ZergRush.Alive;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public enum ValueVrapperType
        {
            None,
            Cell,
            LivableSlot,
            Nullable,
        }

        static Mode GenMode(this Type t)
        {
            return t.IsControllable() ? Mode.PartialClass : Mode.ExtensionMethod;
        }

        public static GenTaskCustomImpl GetCustomImplAttr(this Type t)
        {
            var current = t;
            while (current != null)
            {
                var custom = current.CustomImplementations.FirstOrDefault();
                if (custom != null && (ReferenceEquals(current, t) || custom.Inheritable))
                {
                    return custom.ToAttribute();
                }

                if ((current.Options & ZRTypeOption.DoNotInheritGenTags) != 0) break;
                current = current.BaseType;
            }

            return null;
        }

        static Dictionary<Type, GenTaskFlags> genFlagsCache = new Dictionary<Type, GenTaskFlags>();

        public static GenTaskFlags ReadGenFlags(this Type t)
        {
            if (t == null) return GenTaskFlags.None;
            if (genFlagsCache.TryGetValue(t, out var flagsCached)) return flagsCached;

            var flags = t.Flags;
            if ((t.Options & ZRTypeOption.DoNotInheritGenTags) == 0 && t.BaseType != null)
            {
                flags |= t.BaseType.ReadGenFlags();
            }

            flags &= ~t.IgnoreFlags;
            genFlagsCache[t] = flags;
            return flags;
        }

        public static GenTaskFlags ReadGenCustomFlags(this Type t)
        {
            return t.CustomImplementFlags;
        }

        static bool CanPerform(this Type t, GenTaskFlags flags)
        {
            if ((flags & GenTaskFlags.Serialization) != 0 &&
                t.Interfaces.Any(i => i.Name == nameof(IBinaryDeserializable)))
            {
                flags ^= GenTaskFlags.Serialization;
            }

            if ((flags & GenTaskFlags.UpdateFrom) != 0 &&
                t.Interfaces.Any(i => i.Name.StartsWith("IUpdatableFrom", StringComparison.Ordinal)))
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

            if ((flags & GenTaskFlags.Hash) != 0 && t.Interfaces.Any(i => i.Name == nameof(IHashable)))
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
            if (t == null) return;
            if (t == typeof(object)) return;
            if (t == Void || t.IsPrimitive || t.IsNullable() || t.IsEnum || t.IsGenericParameter || t == typeof(string) ||
                t == typeof(byte[]) || t == typeof(Guid) || t == typeof(DateTime)) return;

            if (t.IsArray && t.GetArrayRank() > 1)
            {
                Error($"Multidimensional array is not supported {t} requested from: {requester}");
                return;
            }

            RegisterTypeContext(t, requester);
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

            if (typeGenRequested.TryGetValue(t.NakedGenericDefinition(), out var registered))
            {
                if ((flags & ~registered) == 0) return;
            }

            if (t.IsInterface) return;

            var flagsCheckType = t;
            if (t.IsRef()) return;

            if (t.IsCell() || t.IsLivableSlot() || t.IsList() || t.IsArray)
            {
                var typeArg = t.FirstGenericArg();
                flagsCheckType = typeArg;
                if (!typeArg.IsLoadableConfig())
                    RequestGen(typeArg, t, flags);
            }

            flags = DowngradeFlagsIfNeeded(flagsCheckType, flags, GenTaskFlags.PooledUpdateFrom,
                GenTaskFlags.UpdateFrom);
            flags = DowngradeFlagsIfNeeded(flagsCheckType, flags, GenTaskFlags.PooledDeserialize,
                GenTaskFlags.Deserialize);

            if ((flags & (GenTaskFlags.Serialization | GenTaskFlags.UpdateFrom)) != 0 && t.IsArray == false)
            {
                t.CheckParameterlessConstructor(flags);
            }

            if (t.IsControllable() && t.IsGenericType && t.IsValidType())
            {
                genericInstances.TryGetOrNew(t.GetGenericTypeDefinition()).Add(t);
            }

            if (t.IsControllable() && t.IsGenericType == false && t.BaseType is { IsGenericType: true } baseType &&
                baseType.IsControllable())
            {
                RequestGen(baseType, t, flags);
            }

            if (t.IsControllable() && !t.IsLivableList())
            {
                var generationSupportFlags = t.ReadGenFlags();
                var interfaceSupportFlags = flags & ~generationSupportFlags;

                if (t.CanPerform(interfaceSupportFlags) == false)
                {
                    Error($"Type {t} can not perform {interfaceSupportFlags} with its interfaces");
                    return;
                }

                flags &= ~interfaceSupportFlags;
                if (flags == 0) return;

                RegisterPolymorph(t);
            }
            else if (t.IsAbstract)
            {
                Error($"Type {t} is abstract and can not be registered for flags: " + flags);
            }

            if (t.IsAssignableTo(typeof(Livable)) && t.IsLivableGen() == false)
            {
                Error($"type {t} is ancestor of livable but does not have [GenLivable] tag");
            }

            if (typeGenRequested.TryGetValue(t, out registered))
            {
                var newTasks = flags & (~registered);
                if (newTasks != 0)
                {
                    typeGenRequested[t.NakedGenericDefinition()] = registered | flags;
                    tasks.Enqueue(new GenerationTask(t, newTasks));
                }
            }
            else
            {
                typeGenRequested[t.NakedGenericDefinition()] = flags;
                tasks.Enqueue(new GenerationTask(t, flags));
            }
        }

        static Type NakedGenericDefinition(this Type t)
        {
            if (t.IsGenericType && t.GenericArguments.Any(par => par.IsGenericParameter))
                return t.GetGenericTypeDefinition();
            return t;
        }

        static Dictionary<Type, List<ZRData>> membersForCodegenCache = new Dictionary<Type, List<ZRData>>();

        static Dictionary<Type, List<ZRData>> membersForCodegenInheretedCache = new Dictionary<Type, List<ZRData>>();

        public static IEnumerable<ZRData> GetMembersForCodeGen(this Type type,
            GenTaskFlags flagRestriction = GenTaskFlags.None, bool inheretedMembers = false, bool ignoreCheck = true)
        {
            IEnumerable<ZRData> Filter(IEnumerable<ZRData> m)
            {
                return ignoreCheck ? m.Where(member => (member.ingoreFlags & flagRestriction) == 0) : m;
            }

            if (ignoreCheck && inheretedMembers &&
                membersForCodegenInheretedCache.TryGetValue(type, out var resultCached))
                return Filter(resultCached.Select(member => member.Copy()));
            if (ignoreCheck && !inheretedMembers && membersForCodegenCache.TryGetValue(type, out resultCached))
                return Filter(resultCached.Select(member => member.Copy()));

            var members = new List<ZRData>();
            if (inheretedMembers || !type.IsControllable())
            {
                var baseType = type.BaseType;
                if (baseType != null && baseType != typeof(object))
                {
                    members.AddRange(baseType.GetMembersForCodeGen(flagRestriction, true, ignoreCheck: false)
                        .Select(member => member.Copy()));
                }
            }

            members.AddRange(type.DataMembers.Select(member => member.Copy()));

            foreach (var member in members)
            {
                member.realType ??= member.DeclaredType ?? member.type;
                member.insideConfigStorage = type.IsConfigStorage();

                if (member.type.IsLivableSlot()) member.canBeNull = true;
                if (member.type.IsNullable()) member.canBeNull = true;

                member.SetupIsCell();

                if (member.realType.IsLivableSlot()) member.insideLivableContainer = true;
                if (type.IsTuple() && member.type.IsValueType == false) member.canBeNull = true;
            }

            if ((type.Options & ZRTypeOption.DoNotSortFields) == 0)
            {
                members = members.OrderBy(m1 => m1.name).ToList();
            }

            if (ignoreCheck && inheretedMembers) membersForCodegenInheretedCache[type] = members;
            if (ignoreCheck && !inheretedMembers) membersForCodegenCache[type] = members;

            return Filter(members.Select(member => member.Copy()));
        }
    }
}
