using System;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public partial class CodeGen
    {
        public static string CompareFuncName = "CompareCheck";
        public static string CompErrorFunc = "SerializationTools.LogCompError";
        public static string CompNullComp = "SerializationTools.CompareNull";
        public static string CompRef = "SerializationTools.CompareRefs";
        public static string CompClassId = "SerializationTools.CompareClassId";

        public static void CompareCheckValue(MethodBuilder sink, DataInfo info, string otherValueReader)
        {
            if (info.type.IsAlmostPrimitive() || info.type.IsEnum || info.type.IsString())
            {
                sink.content($"if ({info.access} != {otherValueReader}) {CompErrorFunc}(__path, {info.pathName}, {otherValueReader}, {info.access});");
            }
//            else if (info.isStatic && info.type.IsValueType == false && info.type.IsList() == false)
//            {
//                sink.content($"{CompRef}(path, {info.pathName}, {otherValueReader}, {info.access});");
//            }
            else
            {
                if (info.canBeNull)
                {
                    sink.content($"if ({CompNullComp}(__path, {info.pathName}, {info.access}, {otherValueReader})) {{");
                    sink.indent++;
                }
                if (info.type.CanBeAncestor())
                {
                    sink.content($"if ({CompClassId}(__path, {info.pathName}, {info.access}, {otherValueReader})) {{");
                    sink.indent++;
                }
                sink.content($"__path.Push({info.pathName});");
                if (info.type.IsLoadableConfig())
                {
                    sink.content($"if ({info.access}.id != {otherValueReader}.id) {CompErrorFunc}(__path, {info.pathName}, {otherValueReader}.id, {info.access}.id);");
                }
                else
                {
                    RequestGen(info.type, sink.classType, GenTaskFlags.CompareChech);
                    sink.content($"{info.access}.{CompareFuncName}({otherValueReader}, __path);");
                }
                sink.content($"__path.Pop();");
                if (info.type.CanBeAncestor())
                {
                    sink.indent--;
                    sink.content($"}}");
                }
                if (info.canBeNull)
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

            MethodBuilder sink = MakeGenMethod(type, GenTaskFlags.CompareChech, funcPrefix + CompareFuncName, typeof(void),
                $"{updateFromType.RealName(true)} {instanceName}, Stack<string> __path");
            
            if (type.IsList() || type.IsReadOnlyList() || type.IsArray)
            {
                var countName = !type.IsArray ? "Count" : "Length";
                var elemType = type.FirstGenericArg();
                CompareCheckValue(sink, new DataInfo {type = typeof(int), pathLog = $"\"{countName}\"",
                    baseAccess = $"self.{countName}"}, $"{otherName}.{countName}"); 
                sink.content($"var count = Math.Min(self.{countName}, {otherName}.{countName});");
                sink.content($"for (int i = 0; i < count; i++)");
                sink.content($"{{");
                sink.indent++;
                CompareCheckValue(sink, new DataInfo {type = elemType, baseAccess = $"self[i]", pathLog = $"i.ToString()"}, $"{otherName}[i]"); 
                sink.indent--;
                sink.content($"}}");
            }
            else
            {
                if (type.IsControllable())
                {
                    GenClassSink(type)
                        .inheritance($"ICompareChechable<{updateFromType.RealName(true)}>");
                }
                if (type != updateFromType)
                {
                    otherName = instanceCastedName;
                    sink.content($"var {instanceCastedName} = ({type.RealName(true)}){instanceName};");
                }

                var hasMembers = type.ProcessMembers(GenTaskFlags.CompareChech, true, memberInfo =>
                {
                    CompareCheckValue(sink, memberInfo, memberInfo.valueTransformer($"{otherName}.{memberInfo.baseAccess}"));
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
			return t.IsPrimitive || t.IsFix64();
		}
    }
}