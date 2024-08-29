using System;
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

        public static void CompareCheckValue(MethodBuilder sink, DataInfo info, string otherValueReader)
        {
            if (info.type.IsAlmostPrimitive() || info.type.IsEnum || info.type.IsString() || info.immutableData)
            {
                sink.content(
                    $"if ({info.access} != {otherValueReader}) {CompErrorFunc}({HelperName}, {info.pathName}, {PrinterName}, {otherValueReader}, {info.access});");
            }
            else
            {
                string accessSuffix = "";
                if (info.canBeNull)
                {
                    var compNull = info.type.IsNullable() ? CompNullableFunc : CompNullComp;
                    sink.content(
                        $"if ({compNull}({HelperName}, {info.pathName}, {PrinterName}, {info.access}, {otherValueReader})) {{");
                    sink.indent++;
                    if (info.type.IsNullable())
                    {
                        accessSuffix = ".Value";
                    }
                }

                if (info.type.CanBeAncestor())
                {
                    sink.content(
                        $"if ({CompClassId}({HelperName}, {info.pathName}, {PrinterName}, {info.access}, {otherValueReader})) {{");
                    sink.indent++;
                }

                if (info.type.IsMultipleReference())
                {
                    sink.content($"if ({HelperName}.{nameof(ZRCompareCheckHelper.NeedCompareCheck)}({info.pathName}," +
                                 $" {PrinterName}, {info.access}, {otherValueReader})) {{");
                    sink.indent++;
                }

                sink.content($"{HelperName}.Push({info.pathName});");
                if (info.type.IsLoadableConfig())
                {
                    sink.content(
                        $"if ({info.access}{accessSuffix}.id != {otherValueReader}{accessSuffix}.id) {CompErrorFunc}({HelperName}, {info.pathName}, {PrinterName}, {otherValueReader}.id, {info.access}.id);");
                }
                else
                {
                    RequestGen(info.type, sink.classType, GenTaskFlags.CompareChech);
                    sink.content(
                        $"{info.access}{accessSuffix}.{CompareFuncName}({otherValueReader}{accessSuffix}, {HelperName}, {PrinterName});");
                }

                sink.content($"{HelperName}.Pop();");
                if (info.canBeNull)
                {
                    sink.indent--;
                    sink.content($"}}");
                }
                if (info.type.IsMultipleReference())
                {
                    sink.indent--;
                    sink.content($"}}");
                }
                if (info.type.CanBeAncestor())
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
                    CompareCheckValue(sink, new DataInfo
                    {
                        type = typeof(int), pathLog = $"\"{countName}\"",
                        baseAccess = $"self.{countName}"
                    }, $"{otherName}.{countName}");
                    sink.content($"var count = Math.Min(self.{countName}, {otherName}.{countName});");
                    sink.content($"for (int i = 0; i < count; i++)");
                    sink.content($"{{");
                    sink.indent++;
                    CompareCheckValue(sink, new DataInfo
                    {
                        type = elemType, canBeNull = !elemType.IsValueType,
                        baseAccess = $"self[i]", pathLog = $"i.ToString()"
                    }, $"{otherName}[i]");
                    sink.indent--;
                    sink.content($"}}");
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

                    var hasMembers = type.ProcessMembers(GenTaskFlags.CompareChech, true,
                        memberInfo =>
                        {
                            CompareCheckValue(sink, memberInfo,
                                memberInfo.valueTransformer($"{otherName}.{memberInfo.baseAccess}"));
                        });
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
                return t.IsPrimitive || t.IsFix64() || t.IsNullablePrimitive() || t.IsNullableEnum() || t.IsGuid() || t.IsDateTime();
            }
        }
    }