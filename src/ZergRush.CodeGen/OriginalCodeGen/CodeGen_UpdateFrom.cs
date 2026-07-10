using System;
using Type = ZergRush.CodeGen.ZRType;
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
        
        public static void GeneralReadFrom(MethodBuilder sink, ZRData info, Type declaredType,
            string declaredAccess, Type carrierType,
            Action<MethodBuilder, ZRData> baseReadCall, string isNullReader, string classIdReader,
            string directReader, string refInst, bool pooled, Func<Type, string> configIdReader = null,  bool needCreateVar = false,
            bool useTempVarThenAssign = false)
        {
            if (needCreateVar)
            {
                if (!declaredType.HasDataWrapper() || declaredType.IsNullable())
                {
                    sink.content($"{declaredType.RealName(true)} {declaredAccess} = default;");
                }
                else
                {
                    sink.content($"{declaredType.RealName(true)} {declaredAccess} = {NewInstExpr(declaredType, false)};");
                }

                needCreateVar = false;
            }

            if (info.IsNullable)
            {
                var nullableAccess = info.Access;
                var nullableTempVar = TempNameFor(info.Access);
                sink.content($"if ({isNullReader}) {{");
                sink.indent++;
                sink.content($"{nullableAccess} = null;");
                sink.indent--;
                sink.content("}");
                sink.content("else {");
                sink.indent++;
                sink.content($"var {nullableTempVar} = {nullableAccess}.GetValueOrDefault();");

                var valueInfo = new ZRData(nullableTempVar, info.Type,
                    info.Options & ~(ZRDataOption.IsNullable | ZRDataOption.CanBeNull | ZRDataOption.SureIsNull));

                GeneralReadFrom(sink, valueInfo, info.Type, nullableTempVar, carrierType,
                    baseReadCall, "false", classIdReader, directReader,
                    refInst, pooled, configIdReader, false, false);
                sink.content($"{nullableAccess} = {nullableTempVar};");
                sink.indent--;
                sink.content("}");
                return;
            }
            
            var t = info.Type;
            var canBeNull = info.CanBeNull && !t.IsValueType;

            var originalInfo = info;
            var originalDeclaredType = declaredType;
            var originalDeclaredAccess = declaredAccess;
            string tempVar = null; 
            if (useTempVarThenAssign)
            {
                tempVar = TempNameFor(originalInfo.Access);
                sink.content($"var {tempVar} = {originalInfo.Access};");
                info = originalInfo.WithAccess(tempVar);
                declaredType = info.Type;
                declaredAccess = tempVar;
            }
            
            if (originalDeclaredType.IsLivableSlot())
            {
                sink.content($"{originalDeclaredAccess}.{updatemod} = true;");
            }

            var name = info.Access;

            if (t.IsImmutableValueType() && !canBeNull)
            {
                sink.content($"{name} = {directReader};");
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
                if (t.IsConfig() && !info.InsideConfigStorage)
                {
                    ConfigFromId(sink, info, carrierType, configIdReader, false);
                    fromExternalSource = true;
                }
                else
                {
                    if (info.SureIsNull && !info.Type.IsValueType)
                    {
                        CreateNewInstance(sink, info.Type, info.Access, classIdReader, refInst, false,
                            info.CantBeAncestor, carrierType: carrierType);
                    }
                    else
                    {
                        string createNewCondition = "";
                        if (canBeNull || CanBeNullAfterConstruction(info.Type, info.CantBeAncestor))
                        {
                            createNewCondition = $"{name} == null";
                        }

                        string classIdVarName = $"{name.Replace('[', '_').Replace(']', '_').Replace('.', '_')}ClassId";
                        if (t.CanBeAncestor() && !info.CantBeAncestor)
                        {
                            sink.content($"var {classIdVarName} = {classIdReader};");
                            createNewCondition = AddOrCondition(createNewCondition,
                                $"{name}.{PolymorphClassIdGetter} != {classIdVarName}");
                        }
                        if (!info.Type.IsImmutableType() && createNewCondition.Valid())
                        {
                            sink.content($"if ({createNewCondition}) {{");
                            sink.indent++;
                            SinkRemovePostProcess(sink, info, pooled);
                            CreateNewInstance(sink, info.Type, info.Access, classIdVarName, refInst, false,
                                info.CantBeAncestor, carrierType: carrierType);
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
                    sink.content($"{t.ConfigRootType().RealName(true)}.Instance.RegisterConfig({info.Access});");
                }

                if (canBeNull)
                {
                    sink.indent--;
                    sink.content($"}}");
                }
            }

            if (useTempVarThenAssign)
            {
                sink.content($"{originalInfo.Access} = {tempVar};");
            }
            
            if (originalDeclaredType.IsLivableSlot())
            {
                sink.content($"{originalDeclaredAccess}.{updatemod} = false;");
            }
        }

        static string TempNameFor(string access)
        {
            return "__" + access.Replace('.', '_').Replace('-', '_').Replace(' ', '_')
                .Replace('[', '_').Replace(']', '_');
        }

        static string OptVar(bool needOne)
        {
            return needOne ? "var " : "";
        }

        public static void GenUpdateValueFromInstance(MethodBuilder sink, ZRData info, Type declaredType,
            string declaredAccess, Type carrierType, string other, bool pooled,
            bool needCreateVar = false, bool needTempVarThenAssign = false, bool supportMultiRef = true)
        {
            var t = info.Type;
            // info can be transformed because read from can do temp value wrapping for it
            if (supportMultiRef && info.Type.IsMultipleReference() && !needCreateVar)
            {
                needTempVarThenAssign = true;
            }
            Func<ZRData, string> defaultContent = info1 =>
            {
                var baseCall = $"{info1.Access}.{UpdateFuncName}({other}, {HelperName});";
                if (supportMultiRef && info1.Type.IsMultipleReference())
                {
                    if (info1.Type.IsLivableNode() && !info1.Type.IsLivableRoot())
                    {
                        baseCall = $"if (!{HelperName}.TryLoadAlreadyUpdatedLivable({other}, ref {info1.Access}," +
                                   $" {(info.InsideLivableContainer ? "true" : "false")})) {baseCall}";
                    }
                    else
                    {
                        baseCall = $"if (!{HelperName}.TryLoadAlreadyUpdated({other}, ref {info1.Access})) {baseCall}";
                    }
                }
                
                return baseCall;
            };
            Action<MethodBuilder, ZRData> baseReadCall = (s, info1) => s.content(defaultContent(info1));

            if (info.Immutable || info.Type.IsImmutableData())
            {
                if (needCreateVar && declaredType.IsCell())
                {
                    sink.content($"{OptVar(needCreateVar)}{declaredAccess} = {declaredType.NewInstExpr(other, true)};");
                }
                else
                {
                    sink.content($"{OptVar(needCreateVar)}{info.Access} = {other};");
                }
                return;
            }
            else if (info.IsNullable)
            {
                var nullableAccess = info.Access;
                var tempVar = TempNameFor(info.Access);
                sink.content($"if ({other} == null) {{");
                sink.indent++;
                sink.content($"{nullableAccess} = null;");
                sink.indent--;
                sink.content("}");
                sink.content("else {");
                sink.indent++;
                sink.content($"var {tempVar} = {nullableAccess}.GetValueOrDefault();");
                var valueInfo = new ZRData(tempVar, info.Type,
                    info.Options & ~(ZRDataOption.IsNullable | ZRDataOption.CanBeNull));
                GenUpdateValueFromInstance(sink, valueInfo, info.Type, tempVar, carrierType,
                    $"{other}.Value", pooled, supportMultiRef: supportMultiRef);
                sink.content($"{nullableAccess} = {tempVar};");
                sink.indent--;
                sink.content("}");
                return;
            }
            else if (t.IsArray)
            {
                baseReadCall = (s, info1) =>
                {
                    string name = info1.Access;
                    string newCountName = $"{name.Replace('[', '_').Replace(']', '_')}Count";
                    string tempVarName = $"{name.Replace('[', '_').Replace(']', '_')}Temp";
                    s.content($"var {newCountName} = {other}.Length;");
                    s.content($"var {tempVarName} = {info1.Access};");
                    s.content($"Array.Resize(ref {tempVarName}, {newCountName});");
                    s.content($"{info1.Access} = {tempVarName};");
                    s.content(defaultContent(info1));
                };
            }

            RequestGen(declaredType, sink.classType, GenTaskFlags.UpdateFrom);

            GeneralReadFrom(sink, info, declaredType, declaredAccess, carrierType,
                baseReadCall: baseReadCall,
                //arrayLengthReader: $"{other}.Length",
                isNullReader: $"{other} == null",
                classIdReader: $"{other}.{CodeGen.PolymorphClassIdGetter}",
                pooled: pooled,
                refInst: other,
                directReader: other,
                needCreateVar: needCreateVar,
                useTempVarThenAssign: needTempVarThenAssign
            );
            
        }

        public static string ReadNewInstanceOfImmutableType(Type t, bool pooled)
        {
            if (t.IsNullable())
                return ReadNewInstanceOfImmutableType(t.NullableUnderlyingType(), pooled);
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
            return $"Read{t.UniqueName()}()";
        }

        public static void SinkArrayUpdateFromWithFixedSize(MethodBuilder sink, Type type, string prefix, string other,
            bool pooled)
        {
            sink.content($"for (int i = 0; i < {prefix}.Length; i++)");
            sink.content($"{{");
            sink.indent++;
            // if (!type.IsValueType)
            //     sink.content($"if ({other}[i] == null) {{ {prefix}[i] = null; continue; }}");
            var options = type.IsValueType ? ZRDataOption.None : ZRDataOption.CanBeNull;
            var target = type.ToData($"{prefix}[i]", options);
            var source = type.ToData($"{other}[i]").Access;
            GenUpdateValueFromInstance(sink, target, type, $"{prefix}[i]", sink.classType,
                source, pooled);
            sink.indent--;
            sink.content($"}}");
        }

        static string updatemod = "__update_mod";

        public static void SinkUpdateFromList(MethodBuilder sink, Type elementType,
            string accessPrefix, string other, bool pooled, bool useAddCopyFunc)
        {
            if (elementType.IsAssignableTo(typeof(IStableIdentifiable)))
            {
                sink.content($"{accessPrefix}.StableUpdateFrom({other}, {HelperName});");
                return;
            }
            
            var options = elementType.IsValueType ? ZRDataOption.None : ZRDataOption.CanBeNull;
            if (useAddCopyFunc) options |= ZRDataOption.InsideLivableContainer;
            var elementData = elementType.ToData($"{accessPrefix}[i]", options);
            
            var refInst = elementType.ToData($"{other}[i]").Access;
            sink.content($"int i = 0;");
            sink.content($"int oldCount = {accessPrefix}.Count;");
            sink.content($"int crossCount = Math.Min(oldCount, {other}.Count);");
            sink.content($"for (; i < crossCount; ++i)");
            sink.content($"{{");
            sink.indent++;
            GenUpdateValueFromInstance(sink, elementData, elementType, $"{accessPrefix}[i]",
                    sink.classType, refInst, pooled,
                    needTempVarThenAssign: elementType.IsValueType);
                sink.indent--;
            sink.content($"}}");
            sink.content($"for (; i < {other}.Count; ++i)");
            sink.content($"{{");
            sink.indent++;
                if (useAddCopyFunc)
                {
                    CreateNewInstance(sink, elementType, "inst", $"{refInst}.{CodeGen.PolymorphClassIdGetter}",
                        refInst, true, carrierType: sink.classType);
                    sink.content($"self.AddCopy(inst, {refInst}, {HelperName});");
    //                sink.content($"self.Add(null);");
                }
                else
                {
                    var newOptions = ZRDataOption.CanBeNull | ZRDataOption.SureIsNull;
                    if (useAddCopyFunc) newOptions |= ZRDataOption.InsideLivableContainer;
                    GenUpdateValueFromInstance(sink, elementType.ToData("inst", newOptions), elementType,
                        "inst", sink.classType, refInst, needCreateVar: true,
                        pooled: pooled);
                    sink.content($"self.Add(inst);");
                }
            sink.indent--;
            sink.content($"}}");
            sink.content($"for (; i < oldCount; ++i)");
            sink.content($"{{");
            sink.indent++;
                SinkRemovePostProcess(sink,
                    elementType.ToData($"self[{accessPrefix}.Count - 1]"), pooled);
                sink.content($"self.RemoveAt({accessPrefix}.Count - 1);");
            sink.indent--;
            sink.content($"}}");
        }

        public static void SinkUpdateFromDictionary(MethodBuilder sink, Type keyType, Type valueType,
            string accessPrefix, string other, bool pooled)
        {
            // models are in different data trees and if reference is equials it means a really bad alert situation
            // so basically we do not check those
            // sink.content($"if (ReferenceEquals({accessPrefix}, {other})) return;");
            sink.content($"if ({other}.Count == 0) {{ {accessPrefix}.Clear(); return; }}");

            sink.content($"{keyType.RealName(true)}[] __keysToRemove = null;");
            sink.content("int __removeCount = 0;");
            sink.content($"foreach (var __pair in {accessPrefix})");
            sink.content("{");
            sink.indent++;
            sink.content($"if (!{other}.ContainsKey(__pair.Key))");
            sink.content("{");
            sink.indent++;
            sink.content($"__keysToRemove ??= new {keyType.RealName(true)}[{accessPrefix}.Count];");
            sink.content("__keysToRemove[__removeCount++] = __pair.Key;");
            sink.indent--;
            sink.content("}");
            sink.indent--;
            sink.content("}");
            sink.content("for (int __i = 0; __i < __removeCount; ++__i)");
            sink.content("{");
            sink.indent++;
            sink.content($"{accessPrefix}.Remove(__keysToRemove[__i]);");
            sink.indent--;
            sink.content("}");

            sink.content($"foreach (var __pair in {other})");
            sink.content("{");
            sink.indent++;

            if (valueType.IsImmutableData())
            {
                sink.content($"{accessPrefix}[__pair.Key] = __pair.Value;");
            }
            else
            {
                var valueOptions = valueType.IsValueType ? ZRDataOption.None : ZRDataOption.CanBeNull;
                var valueData = valueType.ToData("__value", valueOptions);
                var sourceValue = valueType.ToData("__pair.Value").Access;

                sink.content($"if ({accessPrefix}.TryGetValue(__pair.Key, out var __value))");
                sink.content("{");
                sink.indent++;
                GenUpdateValueFromInstance(sink, valueData, valueType, "__value", sink.classType,
                    sourceValue, pooled,
                    needTempVarThenAssign: valueType.IsValueType);
                sink.indent--;
                sink.content("}");
                sink.content("else");
                sink.content("{");
                sink.indent++;
                var newValueData = valueData.WithOption(ZRDataOption.SureIsNull);
                GenUpdateValueFromInstance(sink, newValueData, valueType, "__value", sink.classType,
                    sourceValue, pooled);
                sink.indent--;
                sink.content("}");
                sink.content($"{accessPrefix}[__pair.Key] = __value;");
            }

            sink.indent--;
            sink.content("}");
        }

        public static void GenUpdateFrom(Type type, bool pooled, string funcPrefix = "")
        {
            const string instanceCastedName = "otherConcrete";
            const string instanceName = "other";

            string otherName = instanceName;
            var flag = GenTaskFlags.UpdateFrom;

            var updateFromType = type.TopParentImplementingFlag(flag) ?? type;
            if (type.IsLivableList()) updateFromType = type;
            
            MethodBuilder sink = MakeGenMethod(type, flag, funcPrefix + UpdateFuncName, typeof(void),
                $"{updateFromType.RealName(true)} {instanceName}, {UpdateFromHelperClassName} {HelperName}");

            if (type.IsValueType)
            {
                sink.extensionRef = true;
            }

            if (type.IsList())
            {
                // For livable list that we do not need to generate in not constructed form
                if (type.IsConstructedGenericType == false) return;
                var elemType = type.GenericTypeArguments[0];
                if (type.IsLivableList())
                {
                    sink.content($"self.{updatemod} = true;");
                }
                SinkUpdateFromList(sink, elemType, type.AccessPrefixInGeneratedFunction(), otherName, pooled,
                    useAddCopyFunc: type.IsLivableList());
                if (type.IsLivableList())
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
                var genericArguments = type.GenericTypeArguments;
                SinkUpdateFromDictionary(sink, genericArguments[0], genericArguments[1],
                    type.AccessPrefixInGeneratedFunction(), otherName, pooled);
            }
            else
            {
                if (type != updateFromType)
                {
                    otherName = instanceCastedName;
                    sink.content($"var {instanceCastedName} = ({type.RealName(true)}){instanceName};");
                    
                    var directUpdateSink = GenClassSink(type).Method(funcPrefix + UpdateFuncName, type, MethodType.Instance, typeof(void),
                        $"{type.RealName(true)} {instanceName}, {UpdateFromHelperClassName} {HelperName}");

                    directUpdateSink.content($"this.UpdateFrom(({updateFromType.RealName(true)})other, {HelperName});");
                    directUpdateSink.classBuilder.inheritance($"IUpdatableFrom<{type.RealName(true)}>");
                }
                if (type.IsControllable())
                {
                    GenClassSink(type)
                        .inheritance($"IUpdatableFrom<{updateFromType.RealName(true)}>");
                }

                var genericOtherName = otherName;
                var genericOptions = GenericMembers(sink);
                genericOptions.beginGenericBranch = branch =>
                {
                    genericOtherName = $"__genericOther{branch.index}";
                    sink.content($"var {genericOtherName} = ({branch.instance.RealName(true)})(object){otherName};");
                };
                type.ProcessMembers(flag, true,
                    (member, memberInfo, declaredAccess) =>
                    {
                        GenUpdateValueFromInstance(sink, memberInfo,
                            member.DeclaredType ?? memberInfo.Type, declaredAccess, type,
                            member.ToData($"{genericOtherName}.{member.Name}").Access, pooled,
                            needTempVarThenAssign: member.Kind == ZRMemberKind.Property ||
                                member.DeclaredType.IsCell() || member.DeclaredType.IsLivableSlot());
                    }, genericOptions);
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
