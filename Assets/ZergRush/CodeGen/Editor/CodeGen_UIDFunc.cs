using System;
using System.Linq;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string UIdFuncName = "UId";
        public static string ConfigRegister = "ConfigRegister";
        public static void GenUIDFunc(Type type, string funcPrefix)
        {
            TraverseGenCustomType(new TraversStrategy
            {
                flag = GenTaskFlags.UIDGen,
                funcName = UIdFuncName,
                needDictKeyTraverse = false,
                interfaceType = typeof(IUniquelyIdentifiable),
                memberPredicate = info => info.sharpMemberInfo.HasAttribute<UIDComponent>(),
                needMembersGenRequest = false,
                start = (sink, baseCall) =>
                {
                    sink.needBaseValCall = false;
                    var start = type.NeedsPolymorphRegistration() ? $"{PolymorphClassIdFunc}()" : RandomHash().ToString();
                    sink.content($"{HashType} hash = {start};");
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

        public static string UIdExpr(DataInfo info)
        {
            var t = info.type;
            var name = info.access;
            if (t == typeof(bool)) return $"{name} ? 1u : 0u";
            if (t.IsPrimitive || t.IsEnum) return $"({HashType}){name}";

            string calcHash = $"{name}.{UIdFuncName}";
            if (t == typeof(string))
            {
                calcHash = $"({HashTypeName}){name}.CalculateHash()";
            }

            if (info.canBeNull)
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
                    if (info.type.IsLoadableConfig())
                    {
                        sink.content($"_collection.AddConfigToRegister({info.access});");
                    }
                    if (info.type.IsCollectableConfigType())
                    {
                        RequestGen(info.type, sink.classType, GenTaskFlags.CollectConfigs);
                        sink.content($"{info.access}{(info.canBeNull ? "?" : "")}.{collectConfigFuncName}(_collection);");
                    }
                },
                memberPredicate = info => info.type.IsList() || info.type.IsDictionary() || (info.type.ReadGenFlags() & GenTaskFlags.CollectConfigs) != 0,
                funcArgs = $"{ConfigRegister} _collection"
            }, type, funcPrefix);
        }
    }
}