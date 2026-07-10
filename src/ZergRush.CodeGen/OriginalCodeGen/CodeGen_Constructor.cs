using System;
using Type = ZergRush.CodeGen.ZRType;
using ZergRush.Alive;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        static bool HasGeneratedDefaultConstructor(this Type type)
        {
            return type.ReadGenFlags().HasFlag(GenTaskFlags.DefaultConstructor);
        }
        
        static bool HasDefaultConstructor(this Type type)
        {
            if (type == typeof(object)) return true;
            return type.ReadGenFlags().HasFlag(GenTaskFlags.DefaultConstructor) || type.GetConstructor(Type.EmptyTypes) != null;
        }
        
        static bool IsLivableAncestor(this Type type)
        {
            return type.IsAssignableTo(typeof(Livable));
        }
        
        static bool CanBeNullAfterConstruction(Type type, bool cantBeAncestor)
        {
            return type.CanBeAncestor() && !cantBeAncestor;
        }
        
        public static void CreateNewInstance(MethodBuilder sink, Type type, string access, string classIdReader,
            string refInst, bool needCreateVar, bool cantBeAncestor = false, object defaultValue = null,
            Type carrierType = null)
        {
            string newExpr = "";
            bool needCast = false;
            if (type == typeof(string))
            {
                newExpr = "string.Empty";
            }
            else if (type.IsArray)
            {
                newExpr = $"Array.Empty<{type.RealName(true).Remove(type.RealName(true).Length - 2)}>()";
            }
            else if (type.CanBeAncestor() && !cantBeAncestor)
            {
                needCast = true;
                if (refInst.Valid())
                {
                    newExpr = $"{refInst}.{PolymorphNewInstOfSameType}()";
                }
                else
                {
                    if (classIdReader.Valid() == false)
                    {
                        if (type.IsAbstract) { return; }
                        newExpr = NewInstExpr(type);
                    }
                    else
                    {
                        var staticTypeCreator = type.PolymorphicClassIdOwner().RealName(true);
                        if (type.IsGenericParameter)
                        {
                            if (type.GetGenericParameterConstraints().TryFind(par => !par.IsInterface, out var hardPar))
                            {
                                staticTypeCreator = hardPar.PolymorphicClassIdOwner().RealName(true);
                            }
                            else
                            {
                                Error($"can't generate new instance construction for unknown type {type} in {carrierType}, constrain this type with some base class like Livable");
                            }
                        }
                        newExpr = $"{staticTypeCreator}.{PolymorphInstanceFuncName}({classIdReader})";
                    }
                }
            }
            else
            {
                if (type.IsAbstract)
                {
                    Error($"Type {type} is abstract but required to have constructor during {sink.classBuilder.name} generation");
                    return;
                }
                newExpr = NewInstExpr(type, defaultValue);
            }

            sink.content($"{(needCreateVar ? "var " : "")}{access} = {(needCast ? $"({type.RealName(true)})" : "")}{newExpr};");
        }

        public static void GenerateConstructor(Type t, string funcPrefix)
        {
            if (t.IsControllable() == false)
            {
                Error($"you can't generate constructor as extension method!");
                return;
            }

            if (t.IsValueType) return;

            MethodBuilder constructor;
            if (string.IsNullOrWhiteSpace(funcPrefix))
                constructor = MakeGenMethod(t, GenTaskFlags.DefaultConstructor, t.ClearName(), null, "");
            else
                constructor = MakeGenMethod(t, GenTaskFlags.DefaultConstructor, funcPrefix + t.ClearName(), Void, "");
            
            constructor.type = MethodType.Instance;

            t.ProcessMembers(GenTaskFlags.DefaultConstructor, false, (member, info, declaredAccess) =>
            {
                var hasWrapper = member.WrapperTypes.Count > 0;
                if (info.Type.IsValueType && !hasWrapper) return;
                if (info.Type.IsEnum && !hasWrapper) return;
                if (info.Type.IsConfig() && !hasWrapper && !info.InsideConfigStorage) return;
                
                // Livable slot can be readonly and kind of can be null at the same time due to value transformer.
                if (info.CanBeNull && member.IsReadOnly && !hasWrapper)
                {
                    Error($"{info} in type {t} can't be marked readonly and have CanBeNull tag at the same time");
                    return;
                }

                if (!info.JustData && !member.IsReadOnly && info.Type.IsLivableCustomType())
                {
                    Error($"{info} in type {t} is livable and can be presented only as readonly field, If you want to change this field runtime use LivableSlot, may be you need to use [JustData] attribute");
                    return;
                }
                if (!hasWrapper && info.CanBeNull) return;
                // For livables all configs should be set in Prepare method thats why its unnesseseary to generate default config values
                if (!hasWrapper && info.Type.IsLoadableConfig() && t.IsLivableCustomType()) return;

                var declaredType = member.DeclaredType ?? info.Type;
                CreateNewInstance(constructor, declaredType, declaredAccess, null, null, false,
                    info.CantBeAncestor, member.DefaultValue, t);
                InitializeWrappedValues(constructor, member, declaredAccess, info);
            }, GenericMembers(constructor));
            
            if (t.HasAttribute<GenModelRootSetup>())
            {
                constructor.content("root = this;");
                constructor.content($"{SetupHierarchyFuncName}();");
            }
        }

        public static string NewInstExpr(this Type t, object constructorArg = null, bool isCustomExprArg = false)
        {
            if (t.HasDefaultConstructor())
            {
                var arg = constructorArg != null ? ((constructorArg is string && !isCustomExprArg) ? $"\"{constructorArg}\"" :  constructorArg) : "";
                
                if (arg is bool b)
                    arg = b ? "true" : "false";
                
                return $"new {t.RealName(true)}({arg})";
            } 
            else if (t.IsGenericParameter)
            {
                return $"new {t.RealName(true)}()";
            }
            else
            {
                return $"default({t.RealName(true)})";
            }
        }

        public static void SinkRemovePostProcess(MethodBuilder sink, ZRData info, bool pooled)
        {
        }

        static void InitializeWrappedValues(MethodBuilder constructor, ZRMember member, string declaredAccess,
            ZRData info)
        {
            if (member.WrapperTypes.Count == 0) return;

            var declaredType = member.DeclaredType;
            var access = declaredAccess;
            for (var i = 0; i < member.WrapperTypes.Count; ++i)
            {
                var wrapper = member.WrapperTypes[i];
                if (wrapper is not (FieldWrapperType.Cell or FieldWrapperType.LivableSlot)) continue;

                var innerType = declaredType?.FirstGenericArg();
                if (innerType == null) return;

                var nextWrapper = i + 1 < member.WrapperTypes.Count
                    ? member.WrapperTypes[i + 1]
                    : FieldWrapperType.None;
                if (nextWrapper is FieldWrapperType.Cell or FieldWrapperType.LivableSlot)
                {
                    constructor.content($"{access}.value = {innerType.NewInstExpr()};");
                    access += ".value";
                    declaredType = innerType;
                    continue;
                }

                if (nextWrapper == FieldWrapperType.Nullable) return;
                if (!info.CanBeNull && !info.Type.IsValueType)
                {
                    constructor.content($"{access}.value = {info.Type.NewInstExpr()};");
                }
                return;
            }
        }
    }
}
