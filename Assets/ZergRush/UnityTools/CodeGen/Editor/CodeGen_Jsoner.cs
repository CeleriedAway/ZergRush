using System;
using ZergRush.Alive;
using Newtonsoft.Json;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string JsonWriteFuncName = "WriteJson";
        public static string JsonReadFuncName = "ReadFromJson";

        public static string readerClassName = "ZRJsonTextReader";
        public static string writerClassName = "ZRJsonTextWriter";

        static JsonTextWriter writer;
        static JsonTextReader reader;

        public static bool IsFix64(this Type type)
        {
            return type.Name == "Fix64";
        }

        public static bool IsGuid(this Type type)
        {
            return type == typeof(Guid) || type == typeof(Guid?);
        }
        public static bool IsDateTime(this Type type)
        {
            return type == typeof(DateTime);
        }

        public static void WriteJsonValueStatement(MethodBuilder sink, DataInfo info, bool inList,
            bool writeDataNodeAsId = false)
        {
            if (info.realType == null) info.SetupIsCell();
            
            var t = info.type;
            bool isNullable = false;
            if (t.IsNullable())
            {
                isNullable = true;
                t = Nullable.GetUnderlyingType(t);
            }

            if (t.IsConfig() == false) RequestGen(t, sink.classType, GenTaskFlags.JsonSerialization);


            if (info.canBeNull || isNullable)
            {
                sink.content($"if ({info.access} == null)");
                sink.openBrace();
                if (!inList) sink.content($"writer.WritePropertyName(\"{info.name}\");");
                sink.content($"writer.WriteNull();");
                sink.closeBrace();
                sink.content($"else");
                sink.openBrace();
            }

            if (!inList) sink.content($"writer.WritePropertyName(\"{info.name}\");");


            if (t.IsFix64())
            {
                sink.content($"writer.WriteValue({info.access}{(isNullable ? ".Value" : "")}.RawValue);");
                //sink.content($"writer.WriteFixedPreview({info.access}, \"{info.access}\");");
            }
            else if (t == typeof(DateTime))
            {
                sink.content($"writer.WriteValue({info.access}.Ticks);");
            }
            else if (t == typeof(Guid))
            {
                sink.content($"writer.WriteValue({info.access}.ToString());");
            }
            else if (writeDataNodeAsId && typeof(IReferencableFromDataRoot).IsAssignableFrom(t))
            {
                sink.content($"writer.WriteValue({info.access}.id);");
            }
            else if (t == typeof(byte[]))
            {
                sink.content($"writer.WriteValue({info.access}.ToBase64());");
            }
            else if (t.IsConfig() && info.insideConfigStorage == false)
            {
                sink.content($"writer.WriteValue({info.access}.{UIdFuncName}().ToString());");
            }
            else if (t.IsRef())
            {
                sink.content($"writer.WriteValue({info.access}.id);");
            }
            else if (t == typeof(ulong))
            {
                sink.content($"writer.WriteValue({info.access}.ToString());");
            }
            else if (t == typeof(Guid))
            {
                sink.content($"writer.WriteValue({info.access}.ToString());");
            }
            else if (t.IsPrimitive || t.IsString())
            {
                sink.content($"writer.WriteValue({info.access}{(isNullable ? ".Value" : "")});");
            }
            else if (t.IsEnum)
            {
                sink.content($"writer.WriteValue({info.access}.ToString());");
            }
            else if (t.IsMultipleReference())
            {
                sink.content($"writer.WriteObjectWithRef({info.access});");
            }
            else
            {
                sink.content($"{info.access}.{JsonWriteFuncName}(writer);");
            }

            if (info.canBeNull || isNullable)
            {
                sink.closeBrace();
            }
        }

        public static bool IsConfig(this Type t)
        {
            return t.IsChildOf<LoadableConfig>();
        }

        public static void ReadJsonValueStatement(MethodBuilder sink, DataInfo info, bool needCreateVar,
            bool readDataNodeFromId = false, bool useTempVar = false)
        {
            if (info.realType == null) info.SetupIsCell();
            
            var t = info.type;
            if (t.IsConfig() == false) RequestGen(t, sink.classType, GenTaskFlags.JsonSerialization);

            // info can be transformed because read from can do temp value wrapping for it
            Action<MethodBuilder, DataInfo> baseCall = (s, info1) =>
                s.content(
                    $"{(info.type.IsArray ? info1.access + " = " : "")}{info1.access}.{JsonReadFuncName}(reader);");
            if (t.IsMultipleReference())
            {
                baseCall = (s, info1) => s.content($"reader.ReadFromRef(ref {info1.access});");
            }

            if ((t.IsValueType && t.IsControllable() == false) || t == typeof(byte[]) || t.IsGuid())
            {
                baseCall = (s, info1) => s.content($"{info1.access} = {DirectJsonImmutableTypeReader(t)};");
            }
            else if (t != typeof(byte[]) && t.IsImmutableType())
            {
                baseCall = (s, info1) => s.content($"{info1.access} = ({t.RealName()}) reader.Value;");
            }

            if (t.IsFix64() || (t.IsNullable() && Nullable.GetUnderlyingType(t).Name == "Fix64"))
            {
                sink.classBuilder.usingSink("FixMath.NET");
            }

            GeneralReadFrom(sink, info,
                baseReadCall: baseCall,
                isNullReader: $"reader.TokenType == JsonToken.Null",
                pooled: false,
                classIdReader: $"reader.ReadJsonClassId()",
                configIdReader: DirectJsonImmutableTypeReader,
                refInst: "",
                refIdReader: "(int) (long) reader.Value",
                directReader: $"{DirectJsonImmutableTypeReader(t)}",
                needCreateVar: needCreateVar,
                getDataNodeFromRootWithRefId: readDataNodeFromId,
                useTempVarThenAssign: useTempVar || info.isValueWrapper != ValueVrapperType.None &&
                (info.type.IsControllableStruct() || info.type.IsMultipleReference())
            );
        }


        public static string DirectJsonImmutableTypeReader(Type t)
        {
            var str = $"({t.RealName(true)})";
            if (t.IsNullable())
            {
                t = Nullable.GetUnderlyingType(t);
            }

            if (t.IsGuid())
                return $"Guid.Parse((string)reader.Value)";
            if (t == typeof(DateTime))
                return $"new DateTime((Int64)reader.Value)";
            if (t.IsEnum)
                return $"((string)reader.Value).ParseEnum<{t.RealName(true)}>()";
            if (t.Name == "Fix64")
                return $"Fix64.FromRaw((Int64)reader.Value)";
            if (t == typeof(bool))
                return str + $"reader.Value";
            // if (t.IsNullable())
            // {
            //     var valtype = Nullable.GetUnderlyingType(t);
            //     var cast = valtype == typeof(float) || valtype == typeof(double) ? "double" : "Int64";
            //     return $"(reader.Value == null ? ({valtype.RealName()})({cast})reader.Value : ({t.RealName()})null)";
            // }
            if (t == typeof(float))
                return "reader.ReadJsonFloat()";
            if (t == typeof(double))
                return "reader.ReadJsonDouble()";
            if (t == typeof(ulong))
                return $"ulong.Parse((string)reader.Value)";
            if (t.IsPrimitive)
                return str + $"(Int64)reader.Value";
            if (t == typeof(string))
                return str + $"reader.Value";
            if (t == typeof(byte[]))
                return $"((string)reader.Value).FromBase64()";
            return str + $"reader.ReadFromJson{t.UniqueName()}()";
        }

        static void JsonAssertReadStartStatement(MethodBuilder sink, string condition)
        {
            sink.content($"if ({condition}) throw new JsonSerializationException(\"Bad Json Format\");");
        }


        static void GenerateJsonSerialization(Type type, string funcPrefix)
        {
            var classSink = GenClassSink(type);
            classSink.usingSink("System.IO");
            classSink.usingSink("Newtonsoft.Json");

            const string writerName = "writer";
            const string readerName = "reader";

            var retType = typeof(bool);

            var accessPrefix = type.AccessPrefixInGeneratedFunction();
            if (type.IsList() || type.IsArray)
            {
                var listNeedAuxId = type.IsModifiableLivableList();

                var returnTypeForReadMethod = type.IsArray ? type : retType;
                var sinkReader = MakeGenMethod(type, GenTaskFlags.JsonSerialization, funcPrefix + JsonReadFuncName,
                    returnTypeForReadMethod, $"{readerClassName} {readerName}");
                var sinkWriter = MakeGenMethod(type, GenTaskFlags.JsonSerialization, funcPrefix + JsonWriteFuncName,
                    Void, $"{writerClassName} {writerName}");

                var elemType = type.FirstGenericArg();
                if (elemType.IsConfig() == false) RequestGen(elemType, type, GenTaskFlags.JsonSerialization);
                var info = DataInfo.WithTypeAndName(elemType, accessPrefix);
                string count = type.IsList() ? "Count" : "Length";

                // Writer
                sinkWriter.content($"writer.WriteStartArray();");
                if (listNeedAuxId)
                {
                    sinkWriter.content($"writer.WriteValue({info.access}.Id);");
                }

                sinkWriter.content($"for (int i = 0; i < {info.access}.{count}; i++)");
                sinkWriter.content($"{{");
                sinkWriter.indent++;
                var dataInfo = new DataInfo
                {
                    type = info.type, baseAccess = $"{info.access}[i]",
                    insideConfigStorage = type.IsConfigStorage()
                }.SetupIsCell();
                dataInfo.canBeNull = dataInfo.type.IsClass;
                WriteJsonValueStatement(sinkWriter,
                    dataInfo, true);
                sinkWriter.indent--;
                sinkWriter.content($"}}");
                sinkWriter.content($"writer.WriteEndArray();");

                // Reader
                JsonAssertReadStartStatement(sinkReader, $"reader.TokenType != JsonToken.StartArray");
                if (type.IsDataList()) sinkReader.content($"self.{updatemod} = true;");
                if (type.IsArray)
                    sinkReader.content(
                        $"if(self == null || self.Length > 0) self = Array.Empty<{elemType.RealName(true)}>();");
                if (listNeedAuxId)
                {
                    sinkReader.content($"self.Id = (int)reader.ReadAsInt32();");
                }

                sinkReader.content($"while (reader.Read())");
                sinkReader.content($"{{");
                sinkReader.indent++;
                sinkReader.content("if (reader.TokenType == JsonToken.EndArray) { break; }");

                Action checkNull = () =>
                {
                    if (elemType.IsValueType == false)
                    {
                        if (type.IsArray)
                        {
                            sinkReader.content("if (reader.TokenType == JsonToken.Null) { self[self.Length - 1] = null; continue; }");
                        }
                        else
                        {
                            sinkReader.content("if (reader.TokenType == JsonToken.Null) { self.Add(null); continue; }");
                        }
                    }
                };

                Action initArray = () => { sinkReader.content($"Array.Resize(ref self, self.Length + 1);"); };
                if (elemType.IsDataNode())
                {
                    if (type.IsArray)
                    {
                        initArray();
                        if (elemType.IsValueType == false)
                        {
                            sinkReader.content("if (reader.TokenType == JsonToken.Null) { self[self.Length - 1] = null; continue; }");
                        }
                    }
                    else
                    {
                        sinkReader.content($"self.Add(null);");
                        sinkReader.content("if (reader.TokenType == JsonToken.Null) continue;");
                    }

                    string tempVarName = "__temp";
                    ReadJsonValueStatement(sinkReader,
                        new DataInfo
                        {
                            type = elemType, carrierType = type, baseAccess = tempVarName,
                            insideConfigStorage = type.IsConfigStorage(),
                            sureIsNull = true
                        }, true);
                    sinkReader.content($"self[self.{count} - 1] = {tempVarName};");
                }
                else
                {
                    // if (type.IsValueType)
                    // {
                    //     sinkReader.content(type.IsArray ? $"self.Add(null);" : "self[self.Length - 1] = null;");
                    //     sinkReader.content("if (reader.TokenType == JsonToken.Null) continue;");
                    // }

                    if (type.IsArray)
                    {
                        initArray();
                        if (elemType.IsValueType == false)
                        {
                            sinkReader.content("if (reader.TokenType == JsonToken.Null) { self[self.Length - 1] = null; continue; }");
                        }
                    }
                    else
                    {
                        if (elemType.IsValueType == false)
                        {
                            sinkReader.content("if (reader.TokenType == JsonToken.Null) { self.Add(null); continue; }");
                        }
                    }

                    ReadJsonValueStatement(sinkReader, new DataInfo
                    {
                        type = info.type, baseAccess = $"val", carrierType = type,
                        insideConfigStorage = type.IsConfigStorage(), sureIsNull = true
                    }, true);

                    if (type.IsArray)
                    {
                        sinkReader.content($"self[self.Length - 1] = val;");
                    }
                    else
                    {
                        sinkReader.content($"{info.access}.Add(val);");
                    }
                }

                sinkReader.indent--;
                sinkReader.content($"}}");
                if (type.IsDataList()) sinkReader.content($"self.{updatemod} = false;");

                if (type.IsArray) sinkReader.content($"return self;");
                else sinkReader.content("return true;");
            }
            else if (type.IsDictionary())
            {
                var sinkReader = MakeGenMethod(type, GenTaskFlags.JsonSerialization, funcPrefix + JsonReadFuncName,
                    retType, $"{readerClassName} {readerName}");
                var sinkWriter = MakeGenMethod(type, GenTaskFlags.JsonSerialization, funcPrefix + JsonWriteFuncName,
                    Void, $"{writerClassName} {writerName}");
                var keyType = type.FirstGenericArg();
                var valType = type.SecondGenericArg();
                RequestGen(keyType, type, GenTaskFlags.JsonSerialization);
                RequestGen(valType, type, GenTaskFlags.JsonSerialization);

                var infoKey = DataInfo.WithTypeAndName(keyType, "key");
                var infoVal = DataInfo.WithTypeAndName(valType, "val");

                // Writer

                sinkWriter.content($"writer.WriteStartArray();");
                sinkWriter.content($"foreach (var item in {accessPrefix})");
                sinkWriter.openBrace();
                sinkWriter.content($"writer.WriteStartObject();");
                sinkWriter.content($"writer.WritePropertyName(\"key\");");
                WriteJsonValueStatement(sinkWriter,
                    new DataInfo
                        { type = keyType, baseAccess = $"item.Key", insideConfigStorage = type.IsConfigStorage() },
                    true);
                sinkWriter.content($"writer.WritePropertyName(\"value\");");
                WriteJsonValueStatement(sinkWriter,
                    new DataInfo
                        { type = valType, baseAccess = $"item.Value", insideConfigStorage = type.IsConfigStorage() },
                    true);
                sinkWriter.content($"writer.WriteEndObject();");
                sinkWriter.closeBrace();
                sinkWriter.content($"writer.WriteEndArray();");

                // Reader
                JsonAssertReadStartStatement(sinkReader, $"reader.TokenType != JsonToken.StartArray");
                sinkReader.content($"while (reader.Read())");
                sinkReader.openBrace();
                sinkReader.content("if (reader.TokenType == JsonToken.EndArray) { break; }");
                JsonAssertReadStartStatement(sinkReader, $"reader.TokenType != JsonToken.StartObject");
                sinkReader.content($"reader.Read();"); // key prop name
                sinkReader.content($"reader.Read();"); // key content
                ReadJsonValueStatement(sinkReader,
                    new DataInfo
                    {
                        type = keyType, carrierType = type, baseAccess = $"key",
                        insideConfigStorage = type.IsConfigStorage(), sureIsNull = true
                    }, true);
                sinkReader.content($"reader.Read();"); // val prop name
                sinkReader.content($"reader.Read();"); // val content
                ReadJsonValueStatement(sinkReader,
                    new DataInfo
                    {
                        type = valType, carrierType = type, baseAccess = $"val",
                        insideConfigStorage = type.IsConfigStorage(), sureIsNull = true
                    }, true);
                sinkReader.content($"reader.ReadSkipComments();"); // end keyval obj
                sinkReader.content($"{accessPrefix}.Add(key, val);");
                sinkReader.closeBrace();
                sinkReader.content("return true;");
            }
            else
            {
                bool externalMode = !type.IsControllable() || type.IsStruct();
                bool immutableMode = type.IsStruct() && !type.IsControllable();

                string readerFuncName = funcPrefix + JsonReadFuncName + (!externalMode ? "Field" : "") +
                                        (immutableMode ? type.UniqueName() : "");
                var sinkReader = MakeGenMethod(type, GenTaskFlags.JsonSerialization, readerFuncName,
                    immutableMode ? type : retType,
                    $"{(immutableMode ? "this " : "")}{readerClassName} {readerName}{(!externalMode ? ", string __name" : "")}",
                    immutableMode);
                var sinkWriter = MakeGenMethod(type, GenTaskFlags.JsonSerialization,
                    funcPrefix + JsonWriteFuncName + (!externalMode ? "Fields" : ""), Void,
                    $"{writerClassName} {writerName}");

                if (sinkReader.needBaseValCall)
                {
                    sinkReader.content($"if (base.{JsonReadFuncName}Field({readerName}, __name)) return true;");
                    sinkReader.needBaseValCall = false;
                }

                if (immutableMode) sinkReader.content($"var self = new {type.RealName(true)}();");

                if (externalMode)
                {
                    sinkReader.content($"while (reader.Read())");
                    sinkReader.openBrace();
                    sinkReader.content($"if (reader.TokenType == JsonToken.PropertyName)");
                    sinkReader.openBrace();
                    sinkReader.content($"var __name = (string) reader.Value;");
                    sinkReader.content($"reader.Read();");
                    sinkWriter.content($"writer.WriteStartObject();");
                }

                type.ProcessMembers(GenTaskFlags.JsonSerialization, true,
                    info => { WriteJsonValueStatement(sinkWriter, info, false); });

                if (type.IsControllable() && type.IsValueType == false)
                    sinkReader.classBuilder.inheritance("IJsonSerializable");
                sinkReader.content($"switch(__name)");
                sinkReader.openBrace();
                type.ProcessMembers(GenTaskFlags.JsonSerialization, true, info =>
                {
                    sinkReader.content($"case \"{info.name}\":");
                    ReadJsonValueStatement(sinkReader, info, false);
                    sinkReader.content($"break;");
                });
                if (!immutableMode && !externalMode) sinkReader.content($"default: return false; break;");
                sinkReader.closeBrace();

                if (externalMode)
                {
                    sinkReader.closeBrace();
                    sinkReader.content("else if (reader.TokenType == JsonToken.EndObject) { break; }");
                    sinkReader.closeBrace();
                    sinkWriter.content($"writer.WriteEndObject();");
                }

                if (immutableMode) sinkReader.content("return self;");
                if (!immutableMode) sinkReader.content($"return true;");
            }
        }
    }
}