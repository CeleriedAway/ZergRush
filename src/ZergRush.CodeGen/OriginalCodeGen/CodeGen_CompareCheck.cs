using System;
using Type = ZergRush.CodeGen.ZRType;
using Newtonsoft.Json;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public partial class CodeGen
    {
        public static string CompareFuncName = "CompareCheck";
        public static string CompErrorFunc = "CodeGenImplTools.LogCompError";
        public static string CompNullComp = "CodeGenImplTools.CompareNull";
        public static string CompNullableFunc = "CodeGenImplTools.CompareNullable";
        public static string CompRef = "CodeGenImplTools.CompareRefs";
        public static string CompClassId = "CodeGenImplTools.CompareClassId";
        public static string PrinterArg = "Action<string> printer";
        public static string PrinterName = "printer";
        public static string CCHelper = nameof(ZRCompareCheckHelper);

        public static void CompareCheckValue(MethodBuilder sink, ZRData info, string otherValueReader,
            string pathExpression)
        {
            if (info.Type.IsAlmostPrimitive() || info.Type.IsEnum || info.Type.IsString() || info.Immutable)
            {
                sink.content(
                    $"if ({info.Access} != {otherValueReader}) {CompErrorFunc}({HelperName}, {pathExpression}, {PrinterName}, {otherValueReader}, {info.Access});");
            }
            else
            {
                string accessSuffix = "";
                if (info.CanBeNull)
                {
                    var nullableValue = info.IsNullable;
                    var compNull = nullableValue ? CompNullableFunc : CompNullComp;
                    sink.content(
                        $"if ({compNull}({HelperName}, {pathExpression}, {PrinterName}, {info.Access}, {otherValueReader})) {{");
                    sink.indent++;
                    if (nullableValue)
                    {
                        accessSuffix = ".Value";
                    }
                }

                if (info.Type.CanBeAncestor())
                {
                    sink.content(
                        $"if ({CompClassId}({HelperName}, {pathExpression}, {PrinterName}, {info.Access}, {otherValueReader})) {{");
                    sink.indent++;
                }

                if (info.Type.IsMultipleReference())
                {
                    sink.content($"if ({HelperName}.{nameof(ZRCompareCheckHelper.NeedCompareCheck)}({pathExpression}," +
                                 $" {PrinterName}, {info.Access}, {otherValueReader})) {{");
                    sink.indent++;
                }

                sink.content($"{HelperName}.Push({pathExpression});");
                if (info.Type.IsLoadableConfig())
                {
                    sink.content(
                        $"if ({info.Access}{accessSuffix}.id != {otherValueReader}{accessSuffix}.id) {CompErrorFunc}({HelperName}, {pathExpression}, {PrinterName}, {otherValueReader}.id, {info.Access}.id);");
                }
                else
                {
                    RequestGen(info.Type, sink.classType, GenTaskFlags.CompareChech);
                    sink.content(
                        $"{info.Access}{accessSuffix}.{CompareFuncName}({otherValueReader}{accessSuffix}, {HelperName}, {PrinterName});");
                }

                sink.content($"{HelperName}.Pop();");
                if (info.CanBeNull)
                {
                    sink.indent--;
                    sink.content($"}}");
                }
                if (info.Type.IsMultipleReference())
                {
                    sink.indent--;
                    sink.content($"}}");
                }
                if (info.Type.CanBeAncestor())
                {
                    sink.indent--;
                    sink.content($"}}");
                }
            }
        }

        public static void GenerateComparisonFunc(Type type, string funcPrefix)
            {
                const string instanceCastedName = "otherConcrete";
                const string instanceName = "other";

                string otherName = instanceName;

                var updateFromType = type.TopParentImplementingFlag(GenTaskFlags.CompareChech) ?? type;

                MethodBuilder sink = MakeGenMethod(type, GenTaskFlags.CompareChech, funcPrefix + CompareFuncName,
                    typeof(void),
                    $"{updateFromType.RealName(true)} {instanceName}, {CCHelper} {HelperName}, {PrinterArg}");

                if (type.IsList() || type.IsArray)
                {
                    var countName = !type.IsArray ? "Count" : "Length";
                    var elemType = type.FirstGenericArg();
                    CompareCheckValue(sink,
                        new ZRData($"self.{countName}", typeof(int)),
                        $"{otherName}.{countName}", $"\"{countName}\"");
                    sink.content($"var count = Math.Min(self.{countName}, {otherName}.{countName});");
                    sink.content($"for (int i = 0; i < count; i++)");
                    sink.content($"{{");
                    sink.indent++;
                    CompareCheckValue(sink,
                        elemType.ToData("self[i]", elemType.IsValueType ? ZRDataOption.None : ZRDataOption.CanBeNull),
                        $"{otherName}[i]", "i.ToString()");
                    sink.indent--;
                    sink.content($"}}");
                }
                else if (type.IsDictionary())
                {
                    var genericArguments = type.GetGenericArguments();
                    var valueType = genericArguments[1];
                    CompareCheckValue(
                        sink,
                        new ZRData("self.Count", typeof(int)),
                        $"{otherName}.Count",
                        "\"Count\"");
                    sink.content("foreach (var item in self)");
                    sink.openBrace();
                    sink.content($"if (!{otherName}.TryGetValue(item.Key, out var otherValue))");
                    sink.openBrace();
                    sink.content($"{CompErrorFunc}({HelperName}, item.Key.ToString(), {PrinterName}, (object)\"missing\", (object)item.Value);");
                    sink.closeBrace();
                    sink.content("else");
                    sink.openBrace();
                    CompareCheckValue(
                        sink,
                        valueType.ToData("item.Value", valueType.IsValueType ? ZRDataOption.None : ZRDataOption.CanBeNull),
                        "otherValue",
                        "item.Key.ToString()");
                    sink.closeBrace();
                    sink.closeBrace();
                    sink.content($"foreach (var item in {otherName})");
                    sink.openBrace();
                    sink.content("if (!self.ContainsKey(item.Key))");
                    sink.openBrace();
                    sink.content($"{CompErrorFunc}({HelperName}, item.Key.ToString(), {PrinterName}, (object)item.Value, (object)\"missing\");");
                    sink.closeBrace();
                    sink.closeBrace();
                }
                else
                {
                    if (type.IsControllable())
                    {
                        GenClassSink(type)
                            .inheritance($"ICompareCheckable<{updateFromType.RealName(true)}>");
                    }

                    if (type != updateFromType)
                    {
                        otherName = instanceCastedName;
                        sink.content($"var {instanceCastedName} = ({type.RealName(true)}){instanceName};");
                    }

                    var genericOtherName = otherName;
                    var genericOptions = GenericMembers(sink);
                    genericOptions.beginGenericBranch = branch =>
                    {
                        genericOtherName = $"__genericOther{branch.index}";
                        sink.content($"var {genericOtherName} = ({branch.instance.RealName(true)})(object){otherName};");
                    };
                    var hasMembers = type.ProcessMembers(GenTaskFlags.CompareChech, true,
                        (member, memberInfo, _) =>
                        {
                            CompareCheckValue(sink, memberInfo,
                                member.ToData($"{genericOtherName}.{member.Name}").Access,
                                $"\"{member.Name}\"");
                        }, genericOptions);
                    if (!hasMembers && sink.type == MethodType.Override)
                    {
                        sink.doNotGen = true;
                    }
                }
            }

            static void GeneratePrintHash(Type t, string prefix)
            {
            }

            static bool IsAlmostPrimitive(this Type t)
            {
                return t.IsPrimitive || t.IsFix64() || t.IsNullablePrimitive() || t.IsNullableEnum()
                       || t.IsGuid() || t.IsDateTime();
            }
        }
    }
