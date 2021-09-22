using System;
using System.Collections.Generic;
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
            if (t == typeof(byte[]))
                sink.content($"{stream}.WriteByteArray({info.access});");
            else if (t.IsConfig() && info.insideConfigStorage == false)
            {
                sink.content($"{stream}.Write({info.access}.{UIdFuncName}());");
                return;
            }
            else if (t.IsNullable())
                sink.content($"{stream}.Write({info.access});");
            else if (t.IsRef())
                sink.content($"{stream}.Write({info.access}.id);");
            else if (t.IsReferencableDataNode() && writeDataNodeAsId)
                sink.content($"{stream}.Write({info.access}.Id);");
            else if (t.IsPrimitive || t.IsString())
                sink.content($"{stream}.Write({info.access});");
            else if (t.IsEnum)
                sink.content($"{stream}.Write(({t.GetEnumUnderlyingType().Name}){info.access});");
            else
            {
                if (t.CanBeAncestor())
                {
                    sink.content($"{stream}.Write({info.access}.{CodeGen.PolymorphClassIdGetter});");
                }

                sink.content($"{info.access}.{WriteFuncName}({stream});");
            }
        }

        public static bool IsConfigStorage(this Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ConfigStorageList<>) ||
                   t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ConfigStorageDict<,>) ||
                   t.IsConfig() || t.IsLoadableConfig();
        }

        public static void SinkListWriterCode(Type listType, MethodBuilder sink, DataInfo info, string stream)
        {
            sink.content($"{stream}.Write({info.access}.Count);");
            sink.content($"for (int i = 0; i < {info.access}.Count; i++)");
            sink.content($"{{");
            sink.indent++;
            WriteToStreamStatement(sink,
                new DataInfo
                {
                    type = info.type, baseAccess = $"{info.access}[i]", insideConfigStorage = listType.IsConfigStorage()
                }, stream);
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
                new DataInfo {type = keyType, baseAccess = $"item.Key", insideConfigStorage = configStorage}, stream);
            WriteToStreamStatement(sink,
                new DataInfo {type = valType, baseAccess = $"item.Value", insideConfigStorage = configStorage}, stream);
            sink.indent--;
            sink.content($"}}");
        }

        public static void SinkArrayWriterCode(MethodBuilder sink, DataInfo info, string stream)
        {
            sink.content($"{stream}.Write({info.access}.Length);");
            sink.content($"for (int i = 0; i < {info.access}.Length; i++)");
            sink.content($"{{");
            sink.indent++;
            WriteToStreamStatement(sink, new DataInfo {type = info.type, baseAccess = $"{info.access}[i]"}, stream);
            sink.indent--;
            sink.content($"}}");
        }

        static void GenerateSerialize(Type type, string funcPrefix = "")
        {
            GenClassSink(type).usingSink("System.IO");

            const string writerName = "writer";
            var sinkWriter = MakeGenMethod(type, GenTaskFlags.Serialize, funcPrefix + WriteFuncName, Void,
                $"BinaryWriter {writerName}");

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
                type.ProcessMembers(GenTaskFlags.Serialize, true,
                    info => { GenWriteValueToStream(sinkWriter, info, writerName); });
            }
        }

        public static bool IsNullable(this Type t)
        {
            return Nullable.GetUnderlyingType(t) != null;
        }

        public static void GenReadValueFromStream(MethodBuilder sink, DataInfo info, string stream, bool pooled,
            bool needVar = false, bool readDataNodeFromId = false)
        {
            var t = info.type;


            // info can be transformed because read from can do temp value wrapping for it
            Action<MethodBuilder, DataInfo> baseCall = (s, info1) =>
                s.content(
                    $"{info1.access}.{ReadFuncName}({stream}{(pooled && t.HasPooledDeserializeMethod() ? $", pool" : "")});");
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
                getDataNodeFromRootWithRefId: readDataNodeFromId
            );
        }

        static void SinkCountCheck(this MethodBuilder sink, string countVar)
        {
            sink.content($"if({countVar} > 1000) throw new {nameof(ZergRushCorruptedOrInvalidDataLayout)}();");
        }

        public static void SinkListReaderCode(Type listType, MethodBuilder sink, Type type, string path, string stream,
            bool pooled)
        {
            string count = listType.IsList() ? "Count" : "Length";
            if (listType.IsDataList()) sink.content($"{path}.{updatemod} = true;");

            sink.content($"var size = {stream}.ReadInt32();");
            sink.SinkCountCheck("size");
            sink.content($"{path}.Capacity = size;");
            sink.content($"for (int i = 0; i < size; i++)");
            sink.content($"{{");
            sink.indent++;
            if (type.IsDataNode())
            {
                sink.content($"self.Add(null);");
                GenReadValueFromStream(sink,
                    new DataInfo
                    {
                        type = type, carrierType = listType, baseAccess = $"self[self.{count} - 1]",
                        insideConfigStorage = listType.IsConfigStorage(),
                        sureIsNull = true
                    }, stream, pooled, false);
            }
            else
            {
                GenReadValueFromStream(sink,
                    new DataInfo
                    {
                        type = type, carrierType = listType, baseAccess = $"val", sureIsNull = true,
                        insideConfigStorage = listType.IsConfigStorage()
                    },
                    stream, pooled, true);
                sink.content($"self.Add(val);");
            }

            sink.indent--;
            sink.content($"}}");
            if (listType.IsDataList()) sink.content($"{path}.{updatemod} = false;");
        }

        public static void SinkDictReaderCode(MethodBuilder sink, Type keyType, Type valType, string path,
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
                    {type = keyType, baseAccess = $"key", sureIsNull = true, insideConfigStorage = configStorage},
                stream, pooled);
            sink.content($"var val = default({valType.RealName(true)});");
            GenReadValueFromStream(sink,
                new DataInfo
                    {type = valType, baseAccess = $"val", sureIsNull = true, insideConfigStorage = configStorage},
                stream,
                pooled);
            if (configStorage)
                sink.content($"{path}.Add(val);"); // ConfigStorageDict must use id as a key.
            else
                sink.content($"{path}.Add(key, val);");
            sink.indent--;
            sink.content($"}}");
        }

        public static void SinkArrayReaderCode(MethodBuilder sink, Type type, string path, string stream, bool pooled)
        {
            path = "array";
            sink.content($"var size = {stream}.ReadInt32();");
            sink.SinkCountCheck("size");
            sink.content($"var {path} = new {type.RealName(true)}[size];");
            sink.content($"for (int i = 0; i < size; i++)");
            sink.content($"{{");
            sink.indent++;
            GenReadValueFromStream(sink, new DataInfo {type = type, baseAccess = $"{path}[i]", sureIsNull = true},
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
                    $"this BinaryReader {readerName}{type.OptPoolSecondArgDecl(pooled)}", disablebleFirstArg: true);
            }
            else
            {
                sinkReader = MakeGenMethod(type, flag, funcPrefix + ReadFuncName, Void,
                    $"BinaryReader {readerName}{type.OptPoolSecondArgDecl(pooled)}");
            }


            var accessPrefix = type.AccessPrefixInGeneratedFunction();
            if (type.IsList() && type.IsGenericOfType(typeof(RefListMk2<>)) == false)
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
                SinkDictReaderCode(sinkReader, keyType, valType, accessPrefix, readerName, pooled,
                    type.IsConfigStorage());
            }
            else
            {
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
                var requesters = typeRequestMap[t];
                foreach (var requester in requesters)
                {
                    var requesterRootConfig = requester.ConfigRootType();
                    if (requesterRootConfig != null) return requesterRootConfig;
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
                //TODO fix, right now it is difficult to reach generation hierarchy and cleary undeerstand config loading type for a field
                throw new NotImplementedException();
                //return;
            }

            sink.content(
                $"{OptVar(needCreateVar)}{info.access} = ({info.type.RealName(true)}){configType.NameWithNamespace()}.GetConfig({idReader(type)});");
        }
    }
}