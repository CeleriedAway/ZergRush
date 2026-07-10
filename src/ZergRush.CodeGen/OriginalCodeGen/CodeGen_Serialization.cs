using System;
using Type = ZergRush.CodeGen.ZRType;
using System.Collections.Generic;
using System.Reflection;
using ZergRush.Alive;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string WriteFuncName = "Serialize";

        public static void GenWriteValueToStream(MethodBuilder sink, ZRData info, string stream)
        {
            if (info.CanBeNull)
                GenWriteNullableToStream(sink, info, stream);
            else WriteToStreamStatement(sink, info, stream);
        }

        public static void GenWriteNullableToStream(MethodBuilder sink, ZRData info, string stream)
        {
            sink.content($"if (!({info.HasValueExpression})) {stream}.Write(false);");
            sink.content($"else {{");
            sink.indent++;
            sink.content($"{stream}.Write(true);");
            WriteToStreamStatement(sink, info, stream);
            sink.indent--;
            sink.content($"}}");
        }

        public static void WriteToStreamStatement(MethodBuilder sink, ZRData info, string stream)
        {
            var t = info.Type;
            var access = info.ReadAccess;

            if (t == typeof(byte[]))
                sink.content($"{stream}.WriteByteArray({access});");
            else if (t.IsConfig() && !info.InsideConfigStorage)
            {
                sink.content($"{stream}.Write({access}.{UIdFuncName}());");
                return;
            }
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
                    sink.content($"{stream}.WriteObjectWithRef({access});");
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

        public static void SinkListWriterCode(Type listType, MethodBuilder sink, Type elementType,
            string access, string stream)
        {
            sink.content($"{stream}.Write({access}.Count);");
            sink.content($"for (int i = 0; i < {access}.Count; i++)");
            sink.content($"{{");
            sink.indent++;
            var options = listType.IsConfigStorage() ? ZRDataOption.InsideConfigStorage : ZRDataOption.None;
            if (!elementType.IsValueType) options |= ZRDataOption.CanBeNull;
            GenWriteValueToStream(sink, elementType.ToData($"{access}[i]", options), stream);
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

            var storageOptions = configStorage ? ZRDataOption.InsideConfigStorage : ZRDataOption.None;
            WriteToStreamStatement(sink, keyType.ToData("item.Key", storageOptions), stream);

            var valueOptions = storageOptions;
            if (!valType.IsValueType) valueOptions |= ZRDataOption.CanBeNull;
            GenWriteValueToStream(sink, valType.ToData("item.Value", valueOptions), stream);
            sink.indent--;
            sink.content($"}}");
        }

        public static void SinkArrayWriterCode(MethodBuilder sink, Type elementType, string access, string stream)
        {
            sink.content($"{stream}.Write({access}.Length);");
            sink.content($"for (int i = 0; i < {access}.Length; i++)");
            sink.content($"{{");
            sink.indent++;

            var options = elementType.IsValueType ? ZRDataOption.None : ZRDataOption.CanBeNull;
            GenWriteValueToStream(sink, elementType.ToData($"{access}[i]", options), stream);
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
                SinkListWriterCode(type, sinkWriter, elemType, accessPrefix, writerName);
            }
            else if (type.IsArray)
            {
                var elemType = type.GetElementType();
                RequestGen(elemType, type, GenTaskFlags.Serialize);
                SinkArrayWriterCode(sinkWriter, elemType, accessPrefix, writerName);
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
                    (_, info, _) => { GenWriteValueToStream(sinkWriter, info, writerName); },
                    GenericMembers(sinkWriter));
            }
        }

        public static bool IsTuple(this Type t)
        {
            return t.IsGenericType && t.Name.StartsWith("ValueTuple");
        }

        public static bool IsNullable(this Type t)
        {
            return t?.CommonConstruct == ZRCommonConstruct.Nullable;
        }

        public static bool IsNullableReferenceType(this Type t)
        {
            var underlyingType = t.NullableUnderlyingType();
            if (underlyingType != null && underlyingType.IsClass) return true;
            return false;
        }

        public static bool IsNullableEnum(this Type t)
        {
            var underlyingType = t.NullableUnderlyingType();
            if (underlyingType != null && underlyingType.IsEnum) return true;
            return false;
        }

        public static bool IsNullablePrimitive(this Type t)
        {
            var underlyingType = t.NullableUnderlyingType();
            if (underlyingType != null && underlyingType.IsPrimitive) return true;
            return false;
        }

        public static void GenReadValueFromStream(MethodBuilder sink, ZRData info, Type declaredType,
            string declaredAccess, Type carrierType, string stream, bool pooled, bool needVar = false)
        {
            var t = info.Type;

            // info can be transformed because read from can do temp value wrapping for it
            Action<MethodBuilder, ZRData> baseCall = (s, info1) =>
                s.content(
                    $"{info1.Access}.{ReadFuncName}({stream});");

            if (t.IsArray || t.IsImmutableType() || (t.IsValueType && t.IsControllable() == false))
                baseCall = (s, info1) =>
                    s.content($"{info1.Access} = {stream}.{ReadNewInstanceOfImmutableType(t, pooled)};");
            else if (t.IsMultipleReference())
            {
                baseCall = (s, info1) => s.content($"{stream}.ReadFromRef(ref {info1.Access});");
            }

            GeneralReadFrom(sink, info, declaredType, declaredAccess, carrierType,
                baseReadCall: baseCall,
                //arrayLengthReader: $"{stream}.ReadInt32()",
                isNullReader: $"!{stream}.ReadBoolean()",
                configIdReader: keyType => $"{stream}.{ReadNewInstanceOfImmutableType(keyType, false)}",
                pooled: pooled,
                classIdReader: $"{stream}.{ReadNewInstanceOfImmutableType(PolymorphClassIdType, pooled)}",
                refInst: "",
                directReader: $"{stream}.{ReadNewInstanceOfImmutableType(t, pooled)}",
                needCreateVar: needVar,
                useTempVarThenAssign: declaredType.HasDataWrapper() &&
                                      info.Type.IsControllableStruct() ||
                                      (info.Type.IsMultipleReference() && !needVar)
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
            if (listType.IsLivableList()) sink.content($"{path}.{updatemod} = true;");

            sink.content($"var size = {stream}.ReadInt32();");
            sink.SinkCountCheck("size");
            if (!listType.IsReactiveCollection())
            {
                sink.content($"{path}.Capacity = size;");
            }
            sink.content($"for (int i = 0; i < size; i++)");
            sink.content($"{{");
            sink.indent++;
            if (listType.IsLivableList())
            {
                sink.content($"self.Add(null);");
                sink.content($"if (!{stream}.ReadBoolean()) continue;");
                var access = $"self[self.{count} - 1]";
                var options = ZRDataOption.SureIsNull;
                if (listType.IsConfigStorage()) options |= ZRDataOption.InsideConfigStorage;
                GenReadValueFromStream(sink, type.ToData(access, options), type, access, listType,
                    stream, pooled, false);
            }
            else
            {
                if (!type.IsValueType)
                    sink.content($"if (!{stream}.ReadBoolean()) {{ self.Add(null); continue; }}");
                var options = ZRDataOption.SureIsNull;
                if (listType.IsConfigStorage()) options |= ZRDataOption.InsideConfigStorage;
                GenReadValueFromStream(sink, type.ToData("val", options), type, "val", listType,
                    stream, pooled, true);
                sink.content($"self.Add(val);");
            }

            sink.indent--;
            sink.content($"}}");
            if (listType.IsLivableList()) sink.content($"{path}.{updatemod} = false;");
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
            var keyOptions = ZRDataOption.SureIsNull;
            if (configStorage) keyOptions |= ZRDataOption.InsideConfigStorage;
            GenReadValueFromStream(sink, keyType.ToData("key", keyOptions), keyType, "key", dictType,
                stream, pooled);

            if (!valType.IsValueType)
                sink.content($"if (!{stream}.ReadBoolean()) {{ {path}.Add(key, null); continue; }}");

            sink.content($"var val = default({valType.RealName(true)});");
            var valueOptions = ZRDataOption.SureIsNull;
            if (configStorage) valueOptions |= ZRDataOption.InsideConfigStorage;
            GenReadValueFromStream(sink, valType.ToData("val", valueOptions), valType, "val", dictType,
                stream, pooled);

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
            var access = $"{path}[i]";
            GenReadValueFromStream(sink, type.ToData(access, ZRDataOption.SureIsNull), type, access,
                sink.classType, stream, pooled);
            sink.indent--;
            sink.content($"}}");
            sink.content($"return {path};");
        }

        static void GenerateDeserialize(Type type, bool pooled, string funcPrefix = "")
        {
            GenClassSink(type).usingSink("System.IO");

            const string readerName = "reader";

            var flag = GenTaskFlags.Deserialize;

            MethodBuilder sinkReader = null;
            if (type.GenMode() == Mode.ExtensionMethod && type.IsStruct() || type.IsArray)
            {
                sinkReader = MakeGenMethod(type, flag, $"Read{type.UniqueName()}", type,
                    $"this ZRBinaryReader {readerName}", disablebleFirstArg: true);
            }
            else
            {
                sinkReader = MakeGenMethod(type, flag, funcPrefix + ReadFuncName, Void,
                    $"ZRBinaryReader {readerName}");
            }


            var accessPrefix = type.AccessPrefixInGeneratedFunction();
            if (type.IsList())
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
                    (member, info, declaredAccess) =>
                    {
                        GenReadValueFromStream(sinkReader, info, member.DeclaredType ?? info.Type,
                            declaredAccess, type, readerName, pooled);
                    },
                    GenericMembers(sinkReader));
                if (immutableMode) sinkReader.content("return self;");
            }
        }

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

        static void ConfigFromId(MethodBuilder sink, ZRData info, Type carrierType,
            Func<Type, string> idReader, bool needCreateVar)
        {
            var type = typeof(ulong);
            var configType = carrierType?.ConfigRootType();
            if (configType == null)
            {
                LogSink.errLog($"Can't find config root type for {info.Access} carrier:{carrierType}");
                //TODO fix, right now it is difficult to reach generation hierarchy and cleary undeerstand config loading type for a field
                throw new ZergRushException($"Can't find config type for {info}");
                //return;
            }

            sink.content(
                $"{OptVar(needCreateVar)}{info.Access} = ({info.Type.RealName(true)}){configType.NameWithNamespace()}.GetConfig({idReader(type)});");
        }
    }
}
