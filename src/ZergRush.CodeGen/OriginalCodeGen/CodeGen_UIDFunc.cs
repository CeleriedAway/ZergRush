using System;
using Type = ZergRush.CodeGen.ZRType;
using System.Linq;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string UIdFuncName = "UId";
        public static string ConfigRegister = "ConfigRegister";
        
        
        public static ulong StrHash(ReadOnlySpan<char> array)
        {
            if (array == null) return 1234567;
            ulong hash = 0;
            for (int i = 0; i < array.Length; i++)
            {
                hash += array[i];
                hash += hash << 10;
                hash ^= hash >> 7;
            }

            return hash;
        }
        
        public static void GenUIDFunc(Type type, string funcPrefix)
        {
            TraverseGenCustomType(new TraversStrategy
            {
                flag = GenTaskFlags.UIDGen,
                funcName = UIdFuncName,
                needDictKeyTraverse = false,
                interfaceType = typeof(IUniquelyIdentifiable),
                memberPredicate = (member, _) => member.HasAttribute<UIDComponent>(),
                needMembersGenRequest = false,
                start = (sink, baseCall) =>
                {
                    sink.needBaseValCall = false;
                    if (baseCall)
                    {
                        sink.content($"var hash = base.{UIdFuncName}();");
                        if (sink.classType.HasAttribute<UIDUseClassNameHash>(true))
                        {
                            sink.content($"hash += {StrHash(sink.classType.Name)};");
                            sink.content(HashMixStatement("hash"));
                        }
                    }
                    else
                    {
                        var start = RandomHash().ToString();
                        sink.content($"{HashType} hash = {start};");
                    }
                },
                elemProcess = (sink, info) =>
                {
                    sink.content($"hash += {UIdExpr(info)};");
                    sink.content(HashMixStatement("hash"));
                },
                finish = sink => sink.content("return hash;"),
                funcReturnType = typeof(ulong)
            }, type, funcPrefix);
        }

        public static string UIdExpr(ZRData info)
        {
            var t = info.Type;
            var name = info.Access;
            if (info.IsNullable)
            {
                var valueInfo = info
                    .WithAccess(info.ReadAccess)
                    .WithOption(ZRDataOption.IsNullable, false)
                    .WithOption(ZRDataOption.CanBeNull, false);
                return $"{info.HasValueExpression} ? {UIdExpr(valueInfo)} : {RandomHash()}";
            }

            if (t == typeof(bool)) return $"{name} ? 1u : 0u";
            if (t.IsPrimitive || t.IsEnum) return $"({HashType}){name}";

            string calcHash = $"{name}.{UIdFuncName}";
            if (t == typeof(string))
            {
                calcHash = $"({HashTypeName}){name}.CalculateHash()";
            }

            if (info.CanBeNull)
            {
                return $"{name} != null ? {calcHash} : {RandomHash()}";
            }
            else
            {
                return calcHash;
            }
        }

        static string collectConfigFuncName = "CollectConfigs";

        public static bool IsCollectableConfigType(this Type t)
        {
            return (t.ReadGenFlags() & GenTaskFlags.CollectConfigs) != 0 ||
                   t.IsList() && t.FirstGenericArg().IsCollectableConfigType() ||
                   t.IsDictionary() && t.SecondGenericArg().IsCollectableConfigType();

        }
        public static void GenCollectConfigs(Type type, string funcPrefix)
        {
            TraverseGenCustomType(new TraversStrategy
            {
                flag = GenTaskFlags.CollectConfigs,
                funcName = collectConfigFuncName,
                needDictKeyTraverse = false,
                //needMembersGenRequest = true,
                elemProcess = (sink, info) =>
                {
                    if (info.Type.IsLoadableConfig())
                    {
                        sink.content($"_collection.AddConfigToRegister({info.Access});");
                    }
                    if (info.Type.IsCollectableConfigType())
                    {
                        RequestGen(info.Type, sink.classType, GenTaskFlags.CollectConfigs);
                        sink.content($"{info.Access}{(info.CanBeNull ? "?" : "")}.{collectConfigFuncName}(_collection);");
                    }
                },
                memberPredicate = (_, info) => info.Type.IsList() || info.Type.IsDictionary() ||
                    (info.Type.ReadGenFlags() & GenTaskFlags.CollectConfigs) != 0,
                funcArgs = $"{ConfigRegister} _collection"
            }, type, funcPrefix);
        }
    }
}
