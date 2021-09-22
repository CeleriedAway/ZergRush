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

        static JsonTextWriter writer;
        static JsonTextReader reader;

        public static bool IsFix64(this Type type)
        {
            return type.Name == "Fix64";
        }

        public static void WriteJsonValueStatement(MethodBuilder sink, DataInfo info, bool inList, bool writeDataNodeAsId = false)
        {
            var t = info.type;
            
            if (t.IsConfig() == false) RequestGen(t, sink.classType, GenTaskFlags.JsonSerialization);
            

            if (info.canBeNull)
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
                sink.content($"writer.WriteValue({info.access}.RawValue);");
                sink.content($"writer.WriteFixedPreview({info.access}, \"{info.access}\");");
                return;
            }
            else if (writeDataNodeAsId && t.IsDataNode() && typeof(IReferencableFromDataRoot).IsAssignableFrom(t))
            {
                sink.content($"writer.WriteValue({info.access}.id);");
            }
            else if (t == typeof(byte[]))
            {
                sink.content($"writer.WriteValue({info.access}.ToBase64());");
            }
            else if (t.IsNullable())
            {
                sink.content($"if ({info.access} == null) writer.WriteNull(); " +
                             $"else writer.WriteValue(({Nullable.GetUnderlyingType(info.type).Name}){info.access});");
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
            else if (t.IsPrimitive || t.IsString())
            {
                sink.content($"writer.WriteValue({info.access});");
            }
            else if (t.IsEnum)
            {
                sink.content($"writer.WriteValue({info.access}.ToString());");
            }
            else
            {
                sink.content($"{info.access}.{JsonWriteFuncName}(writer);");
            }
            
            if (info.canBeNull)
            {
                sink.closeBrace();
            }
        }

        public static bool IsConfig(this Type t)
        {
            return t.IsChildOf<LoadableConfig>();
        }
        
        public static void ReadJsonValueStatement(MethodBuilder sink, DataInfo info, bool needCreateVar, bool readDataNodeFromId = false)
        {
            var t = info.type;
            if (t.IsConfig() == false) RequestGen(t, sink.classType, GenTaskFlags.JsonSerialization);

            // info can be transformed because read from can do temp value wrapping for it
            Action<MethodBuilder, DataInfo> baseCall = (s, info1) => s.content( $"{(info.type.IsArray ? info1.access + " = ": "")}{info1.access}.{JsonReadFuncName}(reader);");
            if (t != typeof(byte[]) && t.IsImmutableType())
            {
                baseCall = (s, info1) => s.content($"{info1.access} = ({t.RealName()}) reader.Value;");
            }
            if (t == typeof(byte[]))
            {
                baseCall = (s, info1) => s.content($"{info1.access} = {DirectJsonImmutableTypeReader(t)};");
            }
            
            if (t.Name == "Fix64")
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
                refIdReader:"(int) (long) reader.Value",
                directReader: $"{DirectJsonImmutableTypeReader(t)}",
                needCreateVar: needCreateVar,
                getDataNodeFromRootWithRefId: readDataNodeFromId
            );
        }

        
        public static string DirectJsonImmutableTypeReader(Type t)
        {
            var str = $"({t.RealName(true)})";

            if (t.IsEnum)
                return $"((string)reader.Value).ParseEnum<{t.RealName(true)}>()";
            if (t.Name == "Fix64")
                return $"Fix64.FromRaw((Int64)reader.Value)";
            if (t == typeof(bool))
                return str + $"reader.Value";
            if (t.IsNullable())
            {
                var valtype = Nullable.GetUnderlyingType(t);
                var cast = valtype == typeof(float) || valtype == typeof(double) ? "double" : "Int64";
                return $"(reader.Value == null ? ({valtype.RealName()})({cast})reader.Value : ({t.RealName()})null)";
            }
            if (t == typeof(float) || t == typeof(double))
                return str + $"(double)reader.Value";
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


            var accessPrefix = type.AccessPrefixInGeneratedFunction();
            if (type.IsList() || type.IsArray)
            {
                var listNeedAuxId = type.IsModifiableLivableList();
                
                var returnTypeForReadMethod = type.IsArray ? type : Void;
                var sinkReader = MakeGenMethod(type, GenTaskFlags.JsonSerialization, funcPrefix + JsonReadFuncName, returnTypeForReadMethod, $"JsonTextReader {readerName}");
                var sinkWriter = MakeGenMethod(type, GenTaskFlags.JsonSerialization, funcPrefix + JsonWriteFuncName, Void, $"JsonTextWriter {writerName}");
                
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
                WriteJsonValueStatement(sinkWriter, 
                    new DataInfo{type = info.type, baseAccess = $"{info.access}[i]", insideConfigStorage = type.IsConfigStorage()},
                    true);
                sinkWriter.indent--;
                sinkWriter.content($"}}");
                sinkWriter.content($"writer.WriteEndArray();");
                
                // Reader
                JsonAssertReadStartStatement(sinkReader, $"reader.TokenType != JsonToken.StartArray");
                if (type.IsDataList()) sinkReader.content($"self.{updatemod} = true;");
                if (type.IsArray) sinkReader.content($"if(self == null || self.Length > 0) self = Array.Empty<{elemType.RealName(true)}>();");
                if (listNeedAuxId)
                {
                    sinkReader.content($"self.Id = (int)reader.ReadAsInt32();");
                }
                sinkReader.content($"while (reader.Read())");
                sinkReader.content($"{{");
                sinkReader.indent++;
                sinkReader.content("if (reader.TokenType == JsonToken.EndArray) { break; }");
                Action initArray = () =>
                {
                    sinkReader.content($"Array.Resize(ref self, self.Length + 1);");
                };
                if (elemType.IsDataNode())
                {
                    if (type.IsArray)
                    {
                        initArray();
                    }
                    else
                        sinkReader.content($"self.Add(null);");
                    ReadJsonValueStatement(sinkReader, 
                        new DataInfo {type = elemType, carrierType = type, baseAccess = $"self[self.{count} - 1]", insideConfigStorage = type.IsConfigStorage(),
                            sureIsNull = true}, false);
                }
                else
                {
                    ReadJsonValueStatement(sinkReader, new DataInfo{type = info.type, baseAccess = $"val", carrierType = type,
                        insideConfigStorage = type.IsConfigStorage(), sureIsNull = true}, true);
                    if (type.IsArray)
                    {
                        initArray();
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
            }
            else if (type.IsDictionary())
            {
                var sinkReader = MakeGenMethod(type, GenTaskFlags.JsonSerialization, funcPrefix + JsonReadFuncName, Void, $"JsonTextReader {readerName}");
                var sinkWriter = MakeGenMethod(type, GenTaskFlags.JsonSerialization, funcPrefix + JsonWriteFuncName, Void, $"JsonTextWriter {writerName}");
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
                    WriteJsonValueStatement(sinkWriter, new DataInfo{type = keyType, baseAccess = $"item.Key", insideConfigStorage = type.IsConfigStorage()}, true);
                    sinkWriter.content($"writer.WritePropertyName(\"value\");");
                    WriteJsonValueStatement(sinkWriter, new DataInfo{type = valType, baseAccess = $"item.Value", insideConfigStorage = type.IsConfigStorage()}, true);
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
                    ReadJsonValueStatement(sinkReader, new DataInfo{type = keyType, carrierType = type, baseAccess = $"key", insideConfigStorage = type.IsConfigStorage(), sureIsNull = true}, true);
                    sinkReader.content($"reader.Read();"); // val prop name
                    sinkReader.content($"reader.Read();"); // val content
                    ReadJsonValueStatement(sinkReader, new DataInfo{type = valType, carrierType = type, baseAccess = $"val", insideConfigStorage = type.IsConfigStorage(), sureIsNull = true}, true);
                    sinkReader.content($"reader.Read();"); // end keyval obj
                    sinkReader.content($"{accessPrefix}.Add(key, val);");
                sinkReader.closeBrace();
            }
            else
            {
                bool externalMode = !type.IsControllable() || type.IsStruct();
                bool immutableMode = type.IsStruct() && !type.IsControllable();
                
                string readerFuncName = funcPrefix + JsonReadFuncName + (!externalMode ? "Field" : "") +
                                        (immutableMode ? type.UniqueName() : "");
                var sinkReader = MakeGenMethod(type, GenTaskFlags.JsonSerialization, readerFuncName, immutableMode ? type : Void,
                    $"{(immutableMode ? "this " : "")}JsonTextReader {readerName}{(!externalMode ? ", string __name" : "")}", immutableMode);
                var sinkWriter = MakeGenMethod(type, GenTaskFlags.JsonSerialization, funcPrefix + JsonWriteFuncName + (!externalMode ? "Fields" : ""), Void, $"JsonTextWriter {writerName}");
                
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
                
                type.ProcessMembers(GenTaskFlags.JsonSerialization, true, info =>
                {
                    WriteJsonValueStatement(sinkWriter, info, false);
                });
                
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
                sinkReader.closeBrace();

                if (externalMode)
                {
                    sinkReader.closeBrace();
                    sinkReader.content("else if (reader.TokenType == JsonToken.EndObject) { break; }");
                    sinkReader.closeBrace();
                    sinkWriter.content($"writer.WriteEndObject();");
                }
                
                if (immutableMode) sinkReader.content("return self;");
            }
        }
    }
}
