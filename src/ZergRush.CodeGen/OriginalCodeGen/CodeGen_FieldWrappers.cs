using System;
using Type = ZergRush.CodeGen.ZRType;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        static void GenerateFieldWrappers()
        {
            Dictionary<string, List<__GenReplaceFieldBase>> fieldsCodeReplace =
                new Dictionary<string, List<__GenReplaceFieldBase>>();

            foreach (var typePair in typeGenRequested)
            {
                var type = typePair.Key;
                
                bool updateEvent = type.HasAttribute<GenUpdatedEvent>();
                //if (type.IsGameNode() == false && !type.IsLivableRoot() && !updateEvent) continue;

                var sink = GenClassSink(type);

                if (updateEvent)
                {
                    sink.inheritance("IHasUpdateEvent");
                    sink.content("[GenIgnore] EventStream updated = new EventStream();");
                    sink.content("public IEventStream Updated => updated;");
                }

                Action<Type, string, int, bool, bool> genField = (fType, fName, index, reactive, recordable) =>
                {
                    var recordInteraction = "";
                    var updateInteraction = updateEvent ? "updated.Send();" : "";
                    if (reactive)
                    {
                        sink.usingSink("ZergRush.ReactiveCore");
                        var cellName = $"{fName}Cell";
                        sink.content(
                            $"[GenIgnore] public Cell<{fType.RealName(true)}> {cellName} = new Cell<{fType.RealName(true)}>();");
                        sink.content(
                            $"{fType.RealName(true)} _{fName} {{ get {{ return {cellName}.value; }} set {{ {cellName}.value = value; {recordInteraction} {updateInteraction}}} }}");
                    }
                    else
                    {
                        var backingFieldName = $"__{fName}";
                        sink.content($"[GenIgnore] public {fType.RealName(true)} {backingFieldName};");
                        sink.content(
                            $"{fType.RealName(true)} _{fName} {{ get {{ return {backingFieldName}; }} set {{ {backingFieldName} = value; {recordInteraction} {updateInteraction}}} }}");
                    }
                };
            }

            if (fieldsCodeReplace.Count > 0)
            {
                foreach (var info in fieldsCodeReplace)
                {
                    //Debug.Log($"Type: {info.Key} Field to replace: {info.Value.Select(i => i.name).ToCodeGenListString()}");
                    var filePath = info.Value[0].file;
                    //Debug.Log($"Type: {info.Key} filePath: {filePath}");

                    var allText = File.ReadAllLines(filePath);

                    foreach (var field in info.Value)
                    {
                        var tagLine = allText[field.line - 1];
                        tagLine = tagLine + "[GenInclude]";
                        allText[field.line - 1] = tagLine;

                        var targetLine = allText[field.line];
                        //Debug.Log($"replacing " + targetLine);
                        var isPublic = targetLine.Contains("public") ? "public" : "";
                        var backingFieldName = "_" + field.name;
                        var whiteSpaceCount = 0;
                        ConsumeSpaces(ref whiteSpaceCount, targetLine);
                        targetLine = CodeGenTools.Indent(whiteSpaceCount / 4) +
                                     $"{isPublic} {field.type.RealName(true)} {field.name} {{ get {{ return {backingFieldName}; }} set {{ {backingFieldName} = value;}} }}";
                        //Debug.Log(targetLine);
                        allText[field.line] = targetLine;
                    }

                    File.WriteAllLines(filePath, allText);
                }
            }
        }
    }
}