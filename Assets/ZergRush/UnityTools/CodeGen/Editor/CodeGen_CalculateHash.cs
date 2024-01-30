using System;
using System.Linq;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string HashFuncName = "CalculateHash";
        public static string HashTypeName = "ulong";
        public static Type HashType = typeof(ulong);
        public static string HashHelperName = nameof(ZRHashHelper);

        public static uint RandomHash()
        {
            return (uint) 345093625; // rand.Next();
        }

        public static string HashExpr(DataInfo info)
        {
            var t = info.type;
            var name = info.access;
            if (t.IsArray) {}
            else if (t == typeof(bool)) return $"{name} ? 1u : 0u";
            else if (t.IsPrimitive || t.IsEnum) return $"({HashType}){name}";

            string calcHash = $"{name}.CalculateHash({HelperName})";
            if (t == typeof(string))
            {
                calcHash = $"({HashTypeName}){name}.CalculateHash()";
            }
            else if (t.IsLoadableConfig())
            {
                calcHash = $"({HashTypeName}){name}.{UIdFuncName}()";
            }
            else if (t.IsMultipleReference())
            {
                calcHash = $"{HelperName}.{nameof(ZRHashHelper.CalculateHash)}({name})";
            }
            if (info.canBeNull && !info.type.IsValueType)
            {
                return $"{name} != null ? {calcHash} : {RandomHash()}";
            }
            if(Nullable.GetUnderlyingType(info.type) != null)
            {
                return $"{name}.HasValue ? (ulong){name}.Value.GetHashCode() : {RandomHash()}";
            }
            else
            {
                return calcHash;
            }
        }
        
        static string ClearDot(string prefix)
        {
            return prefix.EndsWith(".") ? prefix.Remove(prefix.Length - 1) : prefix;
        }

        public static string HashMixStatement(string name)
        {
            return $"{name} += {name} << 11; {name} ^= {name} >> 7;";
        }

        public static void GenHashing(Type type, string funcPrefix)
        {
            TraverseGenCustomType(new TraversStrategy
            {
                flag = GenTaskFlags.Hash,
                funcName = HashFuncName,
                needDictKeyTraverse = true,
                interfaceType = typeof(IHashable),
                needMembersGenRequest = true,
                funcArgs = $"{HashHelperName} {HelperName}",
                start = (sink, hasBaseCall) => {
                    if (hasBaseCall)
                    {
                        sink.content($"{HashType} hash = baseVal;");
                    }
                    else
                    {
                        var start = RandomHash();
                        sink.content($"{HashType} hash = {start};");
                    }
                    if (sink.classType.IsAbstract == false)
                    {
                        sink.content($"hash ^= ({HashTypeName}){Math.Abs((int)type.Name.CalculateHash())};");
                        sink.content(HashMixStatement("hash"));
                    }
                },
                elemProcess = (sink, info) => {
                    sink.content($"hash += {HashExpr(info)};");
                    sink.content(HashMixStatement("hash"));
                },
                finish = sink => sink.content("return hash;"),
                funcReturnType = HashType
            }, type, funcPrefix);
        }

        public class TraversStrategy
        {
            public GenTaskFlags flag;

            public string funcName;
            // method, has base call
            public Action<MethodBuilder, bool> start;
            // method, elem type, elem name, 
            public Action<MethodBuilder, DataInfo> elemProcess;
            public bool needDictKeyTraverse;
            public Func<DataInfo, bool> memberPredicate;
            public Action<MethodBuilder> finish;
            public Type interfaceType;
            public string funcArgs;
            public Type funcReturnType;
            public bool needMembersGenRequest;
        }
        
        public static void TraversGenList(TraversStrategy strategy, MethodBuilder sink, Type elemType, string prefix, bool isArray)
        {
            strategy.start?.Invoke(sink, false);
            sink.content($"var size = {prefix}.{(isArray ? "Length" : "Count")};");
            sink.content($"for (int i = 0; i < size; i++)");
            sink.content($"{{");
            sink.indent++;
            strategy.elemProcess(sink, new DataInfo{type = elemType, baseAccess = $"{prefix}[i]", canBeNull = true});
            sink.indent--;
            sink.content($"}}");
            strategy.finish?.Invoke(sink);
        }
        
        public static void TraverseGenDict(TraversStrategy strategy, MethodBuilder sink, Type keyType, Type valType, string path)
        {
            strategy.start?.Invoke(sink, false);
            sink.content($"foreach (var item in {path})");
            sink.content($"{{");
            sink.indent++;
            if (strategy.needDictKeyTraverse) strategy.elemProcess(sink, new DataInfo{type = keyType, baseAccess = "item.Key", canBeNull = true});
            strategy.elemProcess(sink, new DataInfo{type = valType, baseAccess = "item.Value"});
            sink.indent--;
            sink.content($"}}");
            strategy.finish?.Invoke(sink);
        }

        public static void TraverseGenCustomType(TraversStrategy strategy, Type type, string funcPrefix)
        {
            var sink = MakeGenMethod(type, strategy.flag, funcPrefix + strategy.funcName, strategy.funcReturnType ?? Void, strategy.funcArgs ?? "");
            
            if (type.IsList() || type.IsArray)
            {
                var elemType = type.FirstGenericArg();
                RequestGen(elemType, type, strategy.flag);
                TraversGenList(strategy, sink, elemType, type.AccessPrefixInGeneratedFunction(), type.IsArray);
            }
            else if (type.IsDictionary())
            {
                var elemType = type.FirstGenericArg();
                var keyType = type.SecondGenericArg();
                RequestGen(elemType, type, strategy.flag);
                RequestGen(keyType, type, strategy.flag);
                TraverseGenDict(strategy, sink, elemType, keyType, type.AccessPrefixInGeneratedFunction());
            }
            else
            {
                if (strategy.interfaceType != null && type.IsControllable())
                {
                    sink.classBuilder.inheritance(strategy.interfaceType.Name);
                }
                
                strategy.start?.Invoke(sink, type.NeedBaseCallForFlag(strategy.flag));
                type.ProcessMembers(strategy.flag, strategy.needMembersGenRequest, member =>
                {
                    if (strategy.memberPredicate == null || strategy.memberPredicate(member))
                        strategy.elemProcess(sink, member);
                });
                strategy.finish?.Invoke(sink);
            }
        }
    }
}