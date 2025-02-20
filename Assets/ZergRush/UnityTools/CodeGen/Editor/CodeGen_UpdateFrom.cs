﻿using System;
using System.IO;
using System.Reflection;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string ReadFuncName = "Deserialize";
        public static string UpdateFuncName = "UpdateFrom";
        public static string UpdateFromHelperClassName = "ZRUpdateFromHelper";
        public static string HelperName = "__helper";
        public static string UpdateStaticsFuncName = "UpdateStaticFieldsFrom";
        public static string UpdateDynamicsFields = "UpdateInstaceFieldsFrom";
        
        public static void GeneralReadFrom(MethodBuilder sink, DataInfo info,
            Action<MethodBuilder, DataInfo> baseReadCall, string isNullReader, string classIdReader,
            string directReader, string refInst, string refIdReader, bool pooled, Func<Type, string> configIdReader = null,  bool needCreateVar = false,
            bool useTempVarThenAssign = false, bool getDataNodeFromRootWithRefId = false)
        {
            var t = info.type;
            var canBeNull = info.canBeNull && (!t.IsValueType || t.IsNullable());

            var originalInfo = info;
            string tempVar = null; 
            if (useTempVarThenAssign)
            {
                tempVar = "__" + originalInfo.baseAccess.Replace('.', '_').Replace('-', '_').Replace(' ', '_').Replace('[', '_').Replace(']', '_');
                sink.content($"var {tempVar} = {originalInfo.access};");
                info = new DataInfo
                {
                    type = originalInfo.type,
                    baseAccess = tempVar,
                    sureIsNull = info.sureIsNull
                };
            }
            
            if (originalInfo.realType.IsLivableSlot())
            {
                sink.content($"{originalInfo.realAccess}.{updatemod} = true;");
            }

            var name = info.access;

            if (needCreateVar)
            {
                sink.content($"{info.type.RealName(true)} {info.name} = default;");
            }
            
            if (t.IsImmutableValueType())
            {
                sink.content($"{name} = {directReader};");
            }
            else if (t.IsRef())
            {
                if (refIdReader.Valid())
                {
                    sink.content($"{info.access}.id = {refIdReader};");
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                if (canBeNull)
                {
                    sink.content($"if ({isNullReader}) {{");
                    sink.indent++;
                    SinkRemovePostProcess(sink, info, pooled);
                    sink.content($"{name} = null;");
                    sink.indent--;
                    sink.content($"}}");
                    sink.content($"else {{ ");
                    sink.indent++;
                }

                bool fromExternalSource = false;
                if (t.IsConfig() && info.insideConfigStorage == false)
                {
                    ConfigFromId(sink, info, configIdReader, false);
                    fromExternalSource = true;
                }
                else if (getDataNodeFromRootWithRefId && typeof(IReferencableFromDataRoot).IsAssignableFrom(t))
                {
                    sink.content($"{info.access} = ({info.type.RealName(true)})root.Recall({refIdReader});");
                    fromExternalSource = true;
                }
                else
                {
                    if (info.sureIsNull && !info.type.IsValueType)
                    {
                        CreateNewInstance(sink, info, classIdReader, pooled, refInst, false);
                    }
                    else
                    {
                        string createNewCondition = "";
                        if (canBeNull || info.CanBeNullAfterConstruction())
                        {
                            createNewCondition = $"{name} == null";
                        }

                        string classIdVarName = $"{name.Replace('[', '_').Replace(']', '_').Replace('.', '_')}ClassId";
                        if (t.CanBeAncestor() && info.cantBeAncestor == false)
                        {
                            sink.content($"var {classIdVarName} = {classIdReader};");
                            createNewCondition = AddOrCondition(createNewCondition,
                                $"{name}.{PolymorphClassIdGetter} != {classIdVarName}");
                        }
                        if (info.type.IsImmutableType() == false && createNewCondition.Valid())
                        {
                            sink.content($"if ({createNewCondition}) {{");
                            sink.indent++;
                            SinkRemovePostProcess(sink, info, pooled);
                            CreateNewInstance(sink, info, classIdVarName, pooled, refInst, false);
                            sink.indent--;
                            sink.content($"}}");
                        }
                    }
                }

                if (!fromExternalSource)
                {
                    baseReadCall(sink, info);
                }
                
                if (t.IsConfig() && !fromExternalSource)
                {
                    sink.content($"{t.ConfigRootType().RealName(true)}.Instance.RegisterConfig({info.access});");
                }

                if (canBeNull)
                {
                    sink.indent--;
                    sink.content($"}}");
                }
            }

            if (useTempVarThenAssign)
            {
                sink.content($"{originalInfo.access} = {tempVar};");
            }
            
            if (originalInfo.realType.IsLivableSlot())
            {
                sink.content($"{originalInfo.realAccess}.{updatemod} = false;");
            }
        }

        static string OptVar(bool needOne)
        {
            return needOne ? "var " : "";
        }

        public static void GenUpdateValueFromInstance(MethodBuilder sink, DataInfo info, string other, bool pooled,
            bool needCreateVar = false, bool needTempVarThenAssign = false, bool readDataNodeFromRootWithId = false, bool supportMultiRef = true)
        {
            var t = info.type;
            // info can be transformed because read from can do temp value wrapping for it
            if (supportMultiRef && info.type.IsMultipleReference() && !needCreateVar)
            {
                needTempVarThenAssign = true;
            }
            Func<DataInfo, string> defaultContent = info1 =>
            {
                var baseCall = $"{info1.access}.{UpdateFuncName}({other}, {HelperName}{t.OptPoolIfUpdatebleWithPoolSecondArg(pooled)});";
                if (supportMultiRef && info1.type.IsMultipleReference())
                {
                    if (info1.type.IsDataNode())
                    {
                        baseCall = $"if (!{HelperName}.TryLoadAlreadyUpdatedLivable({other}, ref {info1.access}," +
                                   $" {(info.insideLivableContainer ? "true" : "false")})) {baseCall}";
                    }
                    else
                    {
                        baseCall = $"if (!{HelperName}.TryLoadAlreadyUpdated({other}, ref {info1.access})) {baseCall}";
                    }
                }
                
                return baseCall;
            };
            Action<MethodBuilder, DataInfo> baseReadCall = (s, info1) => s.content(defaultContent(info1));

            // if (info.realType.IsLivableSlot())
            // {
            //     sink.content($"{info.realAccess}.{updatemod} = true;");
            // }

            
            if (info.immutableData || t.IsImmutableData())
            {
                sink.content($"{OptVar(needCreateVar)}{info.access} = {other};");
                return;
            }
            else if (t.IsArray)
            {
                baseReadCall = (s, info1) =>
                {
                    string name = info1.name;
                    string newCountName = $"{name.Replace('[', '_').Replace(']', '_')}Count";
                    string tempVarName = $"{name.Replace('[', '_').Replace(']', '_')}Temp";
                    s.content($"var {newCountName} = {other}.Length;");
                    s.content($"var {tempVarName} = {info1.access};");
                    s.content($"Array.Resize(ref {tempVarName}, {newCountName});");
                    s.content($"{info1.access} = {tempVarName};");
                    s.content(defaultContent(info1));
                };
            }

            RequestGen(info.type, sink.classType, pooled ? GenTaskFlags.PooledUpdateFrom : GenTaskFlags.UpdateFrom);

            GeneralReadFrom(sink, info,
                baseReadCall: baseReadCall,
                //arrayLengthReader: $"{other}.Length",
                isNullReader: $"{other} == null",
                //refIdReader: $"{other}.id",
                classIdReader: $"{other}.{CodeGen.PolymorphClassIdGetter}",
                pooled: pooled,
                refInst: other,
                refIdReader: other + ".id",
                directReader: other,
                needCreateVar: needCreateVar,
                useTempVarThenAssign: needTempVarThenAssign,
                getDataNodeFromRootWithRefId: readDataNodeFromRootWithId
            );
            
        }

        public static string ReadNewInstanceOfImmutableType(Type t, bool pooled)
        {
            if (t.IsNullable())
                return ReadNewInstanceOfImmutableType(Nullable.GetUnderlyingType(t), pooled);
            if (t.IsEnum)
                return $"ReadEnum<{t.RealName(true)}>()";
            if (t.IsPrimitive || t == typeof(Guid) || t.IsDateTime())
                return $"Read{t.Name}()";
            if (t == typeof(string))
                return $"ReadString()";
            if (t == typeof(byte[]))
                return "ReadByteArray()";
            if (t.IsFix64())
                return "ReadFix64()";
            return $"Read{t.UniqueName()}({(pooled && t.HasPooledDeserializeMethod() ? $"pool" : "")})";
        }

        public static void SinkArrayUpdateFromWithFixedSize(MethodBuilder sink, Type type, string prefix, string other,
            bool pooled)
        {
            sink.content($"for (int i = 0; i < {prefix}.Length; i++)");
            sink.content($"{{");
            sink.indent++;
            // if (!type.IsValueType)
            //     sink.content($"if ({other}[i] == null) {{ {prefix}[i] = null; continue; }}");
            GenUpdateValueFromInstance(sink, new DataInfo {type = type, baseAccess = $"{prefix}[i]", canBeNull = !type.IsValueType},
                $"{other}[i]", pooled);
            sink.indent--;
            sink.content($"}}");
        }

        static string updatemod = "__update_mod";

        public static void SinkUpdateFromList(MethodBuilder sink, Type elementType,
            string accessPrefix, string other, bool pooled, bool useAddCopyFunc)
        {
            if (typeof(IStableIdentifiable).IsAssignableFrom(elementType))
            {
                sink.content($"{accessPrefix}.StableUpdateFrom({other}, {HelperName});");
                return;
            }
            
            var refInst = $"{other}[i]";
            sink.content($"int i = 0;");
            sink.content($"int oldCount = {accessPrefix}.Count;");
            sink.content($"int crossCount = Math.Min(oldCount, {other}.Count);");
            sink.content($"for (; i < crossCount; ++i)");
            sink.content($"{{");
            sink.indent++;
                GenUpdateValueFromInstance(sink, new DataInfo {
                        type = elementType,
                        baseAccess = $"{accessPrefix}[i]",
                        canBeNull = !elementType.IsValueType,
                        insideLivableContainer = useAddCopyFunc
                    }, refInst,
                    pooled,
                    needTempVarThenAssign: elementType.IsValueType);
                sink.indent--;
            sink.content($"}}");
            sink.content($"for (; i < {other}.Count; ++i)");
            sink.content($"{{");
            sink.indent++;
                if (useAddCopyFunc)
                {
                    CreateNewInstance(sink, DataInfo.WithTypeAndName(elementType, "inst"), $"{refInst}.{CodeGen.PolymorphClassIdGetter}", pooled, refInst, true);
                    sink.content($"self.AddCopy(inst, {refInst}, {HelperName});");
    //                sink.content($"self.Add(null);");
    //                GenUpdateValueFromInstance(sink, new DataInfo {type = elementType, baseAccess = $"self[i]", sureIsNull = true},
    //                    refInst, pooled: pooled);
                }
                else
                {
                    GenUpdateValueFromInstance(sink, new DataInfo {type = elementType,
                            baseAccess = $"inst", canBeNull = true, sureIsNull = true, insideLivableContainer = useAddCopyFunc},
                        refInst, needCreateVar: true,
                        pooled: pooled);
                    sink.content($"self.Add(inst);");
                }
            sink.indent--;
            sink.content($"}}");
            sink.content($"for (; i < oldCount; ++i)");
            sink.content($"{{");
            sink.indent++;
                SinkRemovePostProcess(sink,
                    new DataInfo {type = elementType, baseAccess = $"self[{accessPrefix}.Count - 1]"}, pooled);
                sink.content($"self.RemoveAt({accessPrefix}.Count - 1);");
            sink.indent--;
            sink.content($"}}");
        }

        public static void GenUpdateFrom(Type type, bool pooled, string funcPrefix = "")
        {
            const string instanceCastedName = "otherConcrete";
            const string instanceName = "other";

            string otherName = instanceName;
            var flag = pooled ? GenTaskFlags.PooledUpdateFrom : GenTaskFlags.UpdateFrom;

            var updateFromType = type.TopParentImplementingFlag(flag) ?? type;
            if (type.IsDataList())
            {
                updateFromType = type;
            }
            
            MethodBuilder sink = MakeGenMethod(type, flag, funcPrefix + UpdateFuncName, typeof(void),
                $"{updateFromType.RealName(true)} {instanceName}, {UpdateFromHelperClassName} {HelperName}{(pooled ? ", ObjectPool pool" : "")}");

            if (type.IsValueType)
            {
                sink.extensionRef = true;
            }

            if (type.IsList())
            {
                // For livable list that we do not need to generate in not constructed form
                if (type.IsConstructedGenericType == false) return;
                var elemType = type.GenericTypeArguments[0];
                if (type.IsDataList() || type.IsLivableList())
                {
                    sink.content($"self.{updatemod} = true;");
                    if (type.IsModifiableLivableList())
                    {
                        sink.content($"self.Id = {instanceName}.Id;");
                    }
                }
                SinkUpdateFromList(sink, elemType, type.AccessPrefixInGeneratedFunction(), otherName, pooled,
                    useAddCopyFunc: type.IsDataList());
                if (type.IsDataList() || type.IsLivableList())
                {
                    sink.content($"self.{updatemod} = false;");
                }
            }
            else if (type.IsArray)
            {
                var elemType = type.GetElementType();
                SinkArrayUpdateFromWithFixedSize(sink, elemType, type.AccessPrefixInGeneratedFunction(), otherName,
                    pooled);
            }
            else if (type.IsDictionary())
            {
                Error($"Update from for dictionary ({type}) is not supported");
            }
            else
            {
                if (type != updateFromType)
                {
                    otherName = instanceCastedName;
                    sink.content($"var {instanceCastedName} = ({type.RealName(true)}){instanceName};");
                    
                    var directUpdateSink = GenClassSink(type).Method(funcPrefix + UpdateFuncName, type, MethodType.Instance, typeof(void),
                        $"{type.RealName(true)} {instanceName}, {UpdateFromHelperClassName} {HelperName}{(pooled ? ", ObjectPool pool" : "")}");

                    directUpdateSink.content($"this.UpdateFrom(({updateFromType.RealName(true)})other, {HelperName}{(pooled ? ", pool" : "")});");
                    directUpdateSink.classBuilder.inheritance($"I{(pooled ? "Pooled" : "")}UpdatableFrom<{type.RealName(true)}>");
                }
                if (type.IsControllable())
                {
                    GenClassSink(type)
                        .inheritance($"I{(pooled ? "Pooled" : "")}UpdatableFrom<{updateFromType.RealName(true)}>");
                }

                type.ProcessMembers(flag, true,
                    memberInfo =>
                    {
                        GenUpdateValueFromInstance(sink, memberInfo,
                            memberInfo.valueTransformer($"{otherName}.{memberInfo.baseAccess}"), pooled,
                            needTempVarThenAssign: memberInfo.sharpMemberInfo is PropertyInfo || memberInfo.realType.IsCell() || memberInfo.realType.IsLivableSlot());
                    });
            }
        }
        
        static T GetAttributeIfAny<T>(this MemberInfo info) where T : Attribute
        {
            if (info.HasAttribute<T>())
                return info.GetCustomAttribute<T>();
            return null;
        }

        static string ReadStringTillWhiteSpace(ref int pos, string str)
        {
            var initPos = pos;
            while (str[pos].IsWhiteSpace() == false) pos++;
            return str.Substring(initPos, pos - initPos);
        }

        static bool IsWhiteSpace(this char c)
        {
            return c == ' ' || c == '\r' || c == '\n' || c == '\t';
        }

        static void ConsumeSpaces(ref int pos, string str)
        {
            while (str[pos].IsWhiteSpace()) pos++;
        }


    }
}