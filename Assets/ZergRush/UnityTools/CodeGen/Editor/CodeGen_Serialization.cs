using System;
using System.Collections.Generic;
using System.Reflection;
using ZergRush.Alive;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string WriteFuncName = "Serialize";

        public static void GenWriteValueToStream(MethodBuilder sink, DataInfo info, string stream,
            bool writeDataNodeAsId = false)
        {
            if (info.canBeNull) GenWriteNullableToStream(sink, info, stream, writeDataNodeAsId);
            else WriteToStreamStatement(sink, info, stream, writeDataNodeAsId);
        }

        public static void GenWriteNullableToStream(MethodBuilder sink, DataInfo info, string stream,
            bool writeDataNodeAsId = false)
        {
            sink.content($"if ({info.access} == null) {stream}.Write(false);");
            sink.content($"else {{");
            sink.indent++;
            sink.content($"{stream}.Write(true);");
            WriteToStreamStatement(sink, info, stream, writeDataNodeAsId);
            sink.indent--;
            sink.content($"}}");
        }

        public static void WriteToStreamStatement(MethodBuilder sink, DataInfo info, string stream,
            bool writeDataNodeAsId = false)
        {
            var t = info.type;
            var access = info.access;

            if (info.type.IsNullable() || info.isValueWrapper == ValueVrapperType.Nullable)
            {
                access += ".Value";
            }

            if (t == typeof(byte[]))
                sink.content($"{stream}.WriteByteArray({access});");
            else if (t.IsConfig() && info.insideConfigStorage == false)
            {
                sink.content($"{stream}.Write({access}.{UIdFuncName}());");
                return;
            }
            else if (t.IsRef())
                sink.content($"{stream}.Write({access}.id);");
            else if (t.IsReferencableDataNode() && writeDataNodeAsId)
                sink.content($"{stream}.Write({access}.Id);");
            else if (t.IsPrimitive || t == typeof(Guid) || t.IsString() || t.IsFix64() || t == typeof(DateTime) || t.IsNullablePrimitive())
                sink.content($"{stream}.Write({access});");
            else if (t.IsEnum)
                sink.content($"{stream}.Write(({t.GetEnumUnderlyingType().Name}){access});");
            else
            {
                if (t.CanBeAncestor())
                {
                    sink.content($"{stream}.Write({access}.{CodeGen.PolymorphClassIdGetter});");
                }

                if (t.IsMultipleReference())
                {
                    sink.content($"writer.WriteObjectWithRef({access});");
                }
                else
                {
                    sink.content($"{access}.{WriteFuncName}({stream});");
                }
            }
        }

        public static bool IsConfigStorage(this Type t)
        {
            return t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(ConfigStorageList<>) ||
                                       t.GetGenericTypeDefinition() == typeof(ConfigStorageDict<,>) ||
                                       t.GetGenericTypeDefinition() == typeof(ConfigStorageSlot<>));
        }

        public static void SinkListWriterCode(Type listType, MethodBuilder sink, DataInfo info, string stream)
        {
            sink.content($"{stream}.Write({info.access}.Count);");
            sink.content($"for (int i = 0; i < {info.access}.Count; i++)");
            sink.content($"{{");
            sink.indent++;
            if (!info.type.IsValueType)
            {
                sink.content($"{stream}.Write({info.access}[i] != null);");
                sink.content($"if ({info.access}[i] != null)");
            }

            sink.content("{");
            sink.indent++;
            WriteToStreamStatement(sink,
                new DataInfo
                {
                    type = info.type, baseAccess = $"{info.access}[i]", insideConfigStorage = listType.IsConfigStorage()
                }.SetupIsCell(), stream);
            sink.indent--;
            sink.content("}");
            sink.indent--;
            sink.content($"}}");
        }

        public static void SinkDictWriterCode(MethodBuilder sink, Type keyType, Type valType, string path,
            string stream, bool configStorage)
        {
            sink.content($"{stream}.Write({path}.Count);");
            sink.content($"foreach (var item in {path})");
            sink.content($"{{");
            sink.indent++;

            WriteToStreamStatement(sink,
                new DataInfo { type = keyType, baseAccess = $"item.Key", insideConfigStorage = configStorage }.SetupIsCell(), stream);

            if (!valType.IsValueType)
            {
                sink.content($"{stream}.Write(item.Value != null);");
                sink.content("if (item.Value != null)");
            }

            sink.content("{");
            sink.indent++;
            WriteToStreamStatement(sink,
                new DataInfo { type = valType, baseAccess = $"item.Value", insideConfigStorage = configStorage }.SetupIsCell(),
                stream);

            sink.indent--;
            sink.content("}");
            sink.indent--;
            sink.content($"}}");
        }

        public static void SinkArrayWriterCode(MethodBuilder sink, DataInfo info, string stream)
        {
            sink.content($"{stream}.Write({info.access}.Length);");
            sink.content($"for (int i = 0; i < {info.access}.Length; i++)");
            sink.content($"{{");
            sink.indent++;

            if (!info.type.IsValueType)
            {
                sink.content($"{stream}.Write({info.access}[i] != null);");
                sink.content($"if ({info.access}[i] != null)");
            }

            sink.content("{");
            sink.indent++;
            WriteToStreamStatement(sink, new DataInfo { type = info.type, baseAccess = $"{info.access}[i]" }.SetupIsCell(), stream);
            sink.indent--;
            sink.content("}");
            sink.indent--;
            sink.content($"}}");
        }

        static void GenerateSerialize(Type type, string funcPrefix = "")
        {
            GenClassSink(type).usingSink("System.IO");

            const string writerName = "writer";
            var sinkWriter = MakeGenMethod(type, GenTaskFlags.Serialize, funcPrefix + WriteFuncName, Void,
                $"ZRBinaryWriter {writerName}");

            var accessPrefix = type.AccessPrefixInGeneratedFunction();
            if (type.IsList())
            {
                var elemType = type.GenericTypeArguments[0];
                RequestGen(elemType, type, GenTaskFlags.Serialize);
                SinkListWriterCode(type, sinkWriter, DataInfo.WithTypeAndName(elemType, accessPrefix), writerName);
            }
            else if (type.IsArray)
            {
                var elemType = type.GetElementType();
                RequestGen(elemType, type, GenTaskFlags.Serialize);
                SinkArrayWriterCode(sinkWriter, DataInfo.WithTypeAndName(elemType, accessPrefix), writerName);
            }
            else if (type.IsDictionary())
            {
                var keyType = type.FirstGenericArg();
                var valType = type.SecondGenericArg();
                RequestGen(keyType, type, GenTaskFlags.Serialize);
                RequestGen(valType, type, GenTaskFlags.Serialize);
                SinkDictWriterCode(sinkWriter, keyType, valType, accessPrefix, writerName, type.IsConfigStorage());
            }
            else
            {
                if (type.IsControllable())
                {
                    sinkWriter.classBuilder.inheritance(nameof(IBinarySerializable));
                }

                type.ProcessMembers(GenTaskFlags.Serialize, true,
                    info => { GenWriteValueToStream(sinkWriter, info, writerName); });
            }
        }

        public static bool IsTuple(this Type t)
        {
            return t.IsGenericType && t.Name.StartsWith("ValueTuple");
        }

        public static bool IsNullable(this Type t)
        {
            return Nullable.GetUnderlyingType(t) != null;
        }

        public static bool IsNullableReferenceType(this Type t)
        {
            var underlyingType = Nullable.GetUnderlyingType(t);
            if (underlyingType != null && underlyingType.IsClass) return true;
            return false;
        }

        public static bool IsNullableEnum(this Type t)
        {
            var underlyingType = Nullable.GetUnderlyingType(t);
            if (underlyingType != null && underlyingType.IsEnum) return true;
            return false;
        }

        public static bool IsNullablePrimitive(this Type t)
        {
            var underlyingType = Nullable.GetUnderlyingType(t);
            if (underlyingType != null && underlyingType.IsPrimitive) return true;
            return false;
        }

        public static void GenReadValueFromStream(MethodBuilder sink, DataInfo info, string stream, bool pooled,
            bool needVar = false, bool readDataNodeFromId = false)
        {
            if (info.realType == null) info.SetupIsCell();
            var t = info.type;

            // info can be transformed because read from can do temp value wrapping for it
            Action<MethodBuilder, DataInfo> baseCall = (s, info1) =>
                s.content(
                    $"{info1.access}.{ReadFuncName}({stream}{(pooled && t.HasPooledDeserializeMethod() ? $", pool" : "")});");

            if (t.IsMultipleReference())
            {
                baseCall = (s, info1) => s.content($"{stream}.ReadFromRef(ref {info1.access});");
            }

            if (t.IsArray || t.IsImmutableType() || (t.IsValueType && t.IsControllable() == false))
                baseCall = (s, info1) =>
                    s.content($"{info1.access} = {stream}.{ReadNewInstanceOfImmutableType(t, pooled)};");

            GeneralReadFrom(sink, info,
                baseReadCall: baseCall,
                //arrayLengthReader: $"{stream}.ReadInt32()",
                isNullReader: $"!{stream}.ReadBoolean()",
                configIdReader: keyType => $"{stream}.{ReadNewInstanceOfImmutableType(keyType, false)}",
                pooled: pooled,
                classIdReader: $"{stream}.{ReadNewInstanceOfImmutableType(PolymorphClassIdType, pooled)}",
                refInst: "",
                refIdReader: $"{stream}.{ReadNewInstanceOfImmutableType(RefIdType, pooled)}",
                directReader: $"{stream}.{ReadNewInstanceOfImmutableType(t, pooled)}",
                needCreateVar: needVar,
                getDataNodeFromRootWithRefId: readDataNodeFromId,
                useTempVarThenAssign: info.isValueWrapper != ValueVrapperType.None &&
                                      info.type.IsControllableStruct() ||
                                      (info.type.IsMultipleReference() && !needVar)
            );
        }

        static void SinkCountCheck(this MethodBuilder sink, string countVar)
        {
            // TODO need external info to customize size check count
            // var constrain = elem.sharpMemberInfo.GetCustomAttribute<GenArrayLengthConstraint>();
            // if (constrain != null && constrain.constrainElementCount == -1) return;
            // sink.content($"if({countVar} > {(constrain != null ? constrain.constrainElementCount : 1000)}) throw new {nameof(ZergRushCorruptedOrInvalidDataLayout)}();");
            sink.content($"if({countVar} > 100000) throw new {nameof(ZergRushCorruptedOrInvalidDataLayout)}();");
        }

        public static void SinkListReaderCode(Type listType, MethodBuilder sink, Type type, string path, string stream,
            bool pooled)
        {
            string count = listType.IsList() ? "Count" : "Length";
            if (listType.IsDataList() || listType.IsLivableList()) sink.content($"{path}.{updatemod} = true;");

            sink.content($"var size = {stream}.ReadInt32();");
            sink.SinkCountCheck("size");
            sink.content($"{path}.Capacity = size;");
            sink.content($"for (int i = 0; i < size; i++)");
            sink.content($"{{");
            sink.indent++;
            if (listType.IsLivableList())
            {
                sink.content($"self.Add(null);");
                sink.content($"if (!{stream}.ReadBoolean()) continue;");
                GenReadValueFromStream(sink,
                    new DataInfo
                    {
                        type = type, carrierType = listType, baseAccess = $"self[self.{count} - 1]",
                        insideConfigStorage = listType.IsConfigStorage(), sureIsNull = true
                    }.SetupIsCell(), stream, pooled, false);
            }
            else
            {
                if (!type.IsValueType)
                    sink.content($"if (!{stream}.ReadBoolean()) {{ self.Add(null); continue; }}");
                GenReadValueFromStream(sink,
                    new DataInfo
                    {
                        type = type, carrierType = listType, baseAccess = $"val", sureIsNull = true,
                        insideConfigStorage = listType.IsConfigStorage()
                    }.SetupIsCell(),
                    stream, pooled, true);
                sink.content($"self.Add(val);");
            }

            sink.indent--;
            sink.content($"}}");
            if (listType.IsDataList()) sink.content($"{path}.{updatemod} = false;");
        }

        public static void SinkDictReaderCode(Type dictType,MethodBuilder sink, Type keyType, Type valType, string path,
            string stream, bool pooled, bool configStorage)
        {
            sink.content($"var size = {stream}.ReadInt32();");
            sink.SinkCountCheck("size");
            //sink.content($"{path}.Capacity = size;");
            sink.content($"for (int i = 0; i < size; i++)");
            sink.content($"{{");
            sink.indent++;
            sink.content($"var key = default({keyType.RealName(true)});");
            GenReadValueFromStream(sink,
                new DataInfo
                    { type = keyType, baseAccess = $"key", sureIsNull = true, insideConfigStorage = configStorage, carrierType = dictType}.SetupIsCell(),
                stream, pooled);

            if (!valType.IsValueType)
                sink.content($"if (!{stream}.ReadBoolean()) {{ {path}.Add(key, null); continue; }}");

            sink.content($"var val = default({valType.RealName(true)});");
            GenReadValueFromStream(sink,
                new DataInfo
                    { type = valType, baseAccess = $"val", sureIsNull = true, insideConfigStorage = configStorage, carrierType = dictType }.SetupIsCell(),
                stream,
                pooled);

            // Currently dict is just a dict with custom argument
            // if (configStorage)
            //     sink.content($"{path}.Add(val);"); // ConfigStorageDict must use id as a key.
            // else
            sink.content($"{path}.Add(key, val);");
            sink.indent--;
            sink.content($"}}");
        }

        public static void SinkArrayReaderCode(MethodBuilder sink, Type type, string path, string stream, bool pooled)
        {
            path = "array";
            sink.content($"var size = {stream}.ReadInt32();");
            sink.SinkCountCheck("size");
            if (type.IsArray)
            {
                sink.content($"var {path} = new {type.GetElementType().RealName(true)}[size][];");
            }
            else
            {
                sink.content($"var {path} = new {type.RealName(true)}[size];");
            }
            sink.content($"for (int i = 0; i < size; i++)");
            sink.content($"{{");
            sink.indent++;
            if (!type.IsValueType)
                sink.content($"if (!{stream}.ReadBoolean()) {{ {path}[i] = null; continue; }}");
            GenReadValueFromStream(sink, new DataInfo {type = type, baseAccess = $"{path}[i]", sureIsNull = true}.SetupIsCell(),
                stream, pooled);
            sink.indent--;
            sink.content($"}}");
            sink.content($"return {path};");
        }

        static void GenerateDeserialize(Type type, bool pooled, string funcPrefix = "")
        {
            GenClassSink(type).usingSink("System.IO");

            const string readerName = "reader";

            var flag = pooled ? GenTaskFlags.PooledDeserialize : GenTaskFlags.Deserialize;

            MethodBuilder sinkReader = null;
            if (type.GenMode() == Mode.ExtensionMethod && type.IsStruct() || type.IsArray)
            {
                sinkReader = MakeGenMethod(type, flag, $"Read{type.UniqueName()}", type,
                    $"this ZRBinaryReader {readerName}{type.OptPoolSecondArgDecl(pooled)}", disablebleFirstArg: true);
            }
            else
            {
                sinkReader = MakeGenMethod(type, flag, funcPrefix + ReadFuncName, Void,
                    $"ZRBinaryReader {readerName}{type.OptPoolSecondArgDecl(pooled)}");
            }


            var accessPrefix = type.AccessPrefixInGeneratedFunction();
            if (type.IsList() && type.IsRefList() == false)
            {
                var elemType = type.GenericTypeArguments[0];
                RequestGen(elemType, type, flag);
                SinkListReaderCode(type, sinkReader, elemType, accessPrefix, readerName, pooled);
            }
            else if (type.IsArray)
            {
                var elemType = type.GetElementType();
                RequestGen(elemType, type, flag);
                SinkArrayReaderCode(sinkReader, elemType, accessPrefix, readerName, pooled);
            }
            else if (type.IsDictionary())
            {
                var keyType = type.FirstGenericArg();
                var valType = type.SecondGenericArg();
                RequestGen(keyType, type, flag);
                RequestGen(valType, type, flag);
                SinkDictReaderCode(type, sinkReader, keyType, valType, accessPrefix, readerName, pooled,
                    type.IsConfigStorage());
            }
            else
            {
                if (type.IsControllable())
                {
                    sinkReader.classBuilder.inheritance(nameof(IBinaryDeserializable));
                }

                bool immutableMode = type.IsStruct() && !type.IsControllable();
                if (immutableMode)
                    sinkReader.content($"var self = new {type.RealName(true)}();");
                type.ProcessMembers(flag, true,
                    info => { GenReadValueFromStream(sinkReader, info, readerName, pooled); });
                if (immutableMode) sinkReader.content("return self;");
            }
        }

//        static bool SerializeWithFeatures(DataInfo info, MethodBuilder sink, string stream, bool deserialize)
//        {
//            foreach (var serializationFeature in serializationFeatures)
//            {
//                if (serializationFeature.isApplicableTo(info))
//                {
//                    bool checkNull = !deserialize && info.canBeNull && serializationFeature.write0IfNull;
//                    if (checkNull)
//                    {
//                        sink.content($"if ({info.access} == null) {{{stream}.Write(false);}}");
//                        sink.content($"else {{");
//                        sink.indent++;
//                    }
//                    if (deserialize)
//                        serializationFeature.buildDeserialization(info, sink, stream);
//                    else
//                        serializationFeature.buildSerialization(info, sink, stream);
//                    if (checkNull)
//                    {
//                        sink.indent--;
//                        sink.content($"}}");
//                    }
//                    return true;
//                }
//            }
//            return false;
//        }

        static Type ConfigRootType(this Type t)
        {
            var configType = t.FindTagInHierarchy<ConfigRootType>()?.type;
            if (configType == null)
            {
                if (typeRequestMap.TryGetValue(t, out var requesters))
                {
                    foreach (var requester in requesters)
                    {
                        var requesterRootConfig = requester.ConfigRootType();
                        if (requesterRootConfig != null) return requesterRootConfig;
                    }
                }
                else
                {
                    Error($"type {t} can't be found in requesters array");
                }
            }

            return configType;
        }

        static void ConfigFromId(MethodBuilder sink, DataInfo info, Func<Type, string> idReader, bool needCreateVar)
        {
            var type = typeof(ulong);
            var configType = info.carrierType?.ConfigRootType();
            if (configType == null)
            {
                LogSink.errLog($"Can't find config root type for {info.name} carrier:{info.carrierType}");
                //TODO fix, right now it is difficult to reach generation hierarchy and cleary undeerstand config loading type for a field
                throw new ZergRushException($"Can't find config type for {info}");
                //return;
            }

            sink.content(
                $"{OptVar(needCreateVar)}{info.access} = ({info.type.RealName(true)}){configType.NameWithNamespace()}.GetConfig({idReader(type)});");
        }
    }
}