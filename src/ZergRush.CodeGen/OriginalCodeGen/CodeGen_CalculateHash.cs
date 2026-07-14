using System;
using Type = ZergRush.CodeGen.ZRType;
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
            return (uint)345093625; // rand.Next();
        }

        public static string HashExpr(ZRData info)
        {
            var t = info.Type;
            var name = info.Access;

            if (info.IsNullable)
            {
                var valueInfo = info
                    .WithAccess(info.ReadAccess)
                    .WithOption(ZRDataOption.IsNullable, false)
                    .WithOption(ZRDataOption.CanBeNull, false);
                return $"{name}.HasValue ? {HashExpr(valueInfo)} : {RandomHash()}";
            }

            if (t.IsArray)
            {
            }

            if (t.IsFix64())
            {
                if (t.IsNullable())
                    return $"{name}.HasValue ? ({HashType}){name}.Value.RawValue : {RandomHash()}";
                else
                    return $"({HashType}){name}.RawValue";
            }
            else if (t == typeof(bool)) return $"{name} ? 1u : 0u";
            else if (t == typeof(float)) return $"(ulong)BitConverter.SingleToInt32Bits({name})";
            else if (t == typeof(double)) return $"(ulong)BitConverter.DoubleToInt64Bits({name})";
            else if (t == typeof(decimal)) return $"(ulong){name}.GetHashCode()";
            else if (t.IsPrimitive || t.IsEnum) return $"({HashType}){name}";

            string calcHash = $"{name}.CalculateHash({HelperName})";
            if (t == typeof(string))
            {
                calcHash = $"CodeGenImplTools.CalculateStringHash({name})";
            }
            else if (t == typeof(DateTime))
            {
                calcHash = $"({HashTypeName}){name}.Ticks";
            }
            else if (t.IsLoadableConfig())
            {
                calcHash = $"({HashTypeName}){name}.{UIdFuncName}()";
            }
            else if (t.IsMultipleReference())
            {
                calcHash = $"{HelperName}.{nameof(ZRHashHelper.CalculateHash)}({name})";
            }

            if (info.CanBeNull && !info.Type.IsValueType)
            {
                return $"{name} != null ? {calcHash} : {RandomHash()}";
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
                start = (sink, hasBaseCall) =>
                {
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
                        ulong CalculateHash(string array)
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

                        sink.content($"hash ^= ({HashTypeName}){Math.Abs((int)CalculateHash(type.Name))};");
                        sink.content(HashMixStatement("hash"));
                    }
                },
                elemProcess = (sink, info) =>
                {
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
            public Action<MethodBuilder, ZRData> elemProcess;
            public Action<MethodBuilder, ZRMember, ZRData, string> memberProcess;
            public bool needDictKeyTraverse;
            public Func<ZRMember, ZRData, bool> memberPredicate;
            public Action<MethodBuilder> finish;
            public Type interfaceType;
            public string funcArgs;
            public Type funcReturnType;
            public bool needMembersGenRequest;
        }

        public static void TraversGenList(TraversStrategy strategy, MethodBuilder sink, Type elemType, string prefix,
            bool isArray)
        {
            strategy.start?.Invoke(sink, false);
            sink.content($"var size = {prefix}.{(isArray ? "Length" : "Count")};");
            sink.content($"for (int i = 0; i < size; i++)");
            sink.content($"{{");
            sink.indent++;
            strategy.elemProcess(sink,
                elemType.ToData($"{prefix}[i]", ZRDataOption.CanBeNull));
            sink.indent--;
            sink.content($"}}");
            strategy.finish?.Invoke(sink);
        }

        public static void TraverseGenDict(TraversStrategy strategy, MethodBuilder sink, Type keyType, Type valType,
            string path)
        {
            strategy.start?.Invoke(sink, false);
            sink.content($"foreach (var item in {path})");
            sink.content($"{{");
            sink.indent++;
            if (strategy.needDictKeyTraverse)
                strategy.elemProcess(sink, keyType.ToData("item.Key", ZRDataOption.CanBeNull));
            strategy.elemProcess(sink, valType.ToData("item.Value"));
            sink.indent--;
            sink.content($"}}");
            strategy.finish?.Invoke(sink);
        }

        public static void TraverseGenCustomType(TraversStrategy strategy, Type type, string funcPrefix)
        {
            var sink = MakeGenMethod(type, strategy.flag, funcPrefix + strategy.funcName,
                strategy.funcReturnType ?? Void, strategy.funcArgs ?? "");

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
                type.ProcessMembers(strategy.flag, strategy.needMembersGenRequest, (member, data, declaredAccess) =>
                {
                    if (strategy.memberPredicate == null || strategy.memberPredicate(member, data))
                    {
                        if (strategy.memberProcess != null)
                            strategy.memberProcess(sink, member, data, declaredAccess);
                        else
                            strategy.elemProcess(sink, data);
                    }
                }, GenericMembers(sink));
                strategy.finish?.Invoke(sink);
            }
        }
    }
}
