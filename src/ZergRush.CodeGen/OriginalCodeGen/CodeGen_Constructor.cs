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
        
        static bool CanBeNullAfterConstruction(this ZRData info)
        {
            return info.type.CanBeAncestor() && info.cantBeAncestor == false;
        }
        
        public static void CreateNewInstance(MethodBuilder sink, ZRData info, string classIdReader,
            string refInst, bool needCreateVar, bool wrapType = false)
        {
            // Some bullshit logic here
            // All of this because of value wrapper concept that should be reconsidered
            var t = wrapType ? info.realType : info.type;
            var name = wrapType ? info.realAccess : info.access;
            
            string newExpr = "";
            bool needCast = false;
            if (t == typeof(string))
            {
                newExpr = "string.Empty";
            }
            else if (t.IsArray)
            {
                newExpr = $"Array.Empty<{t.RealName(true).Remove(t.RealName(true).Length - 2)}>()";
            }
            else if (t.CanBeAncestor() && info.cantBeAncestor == false)
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
                        if (t.IsAbstract) { return; }
                        newExpr = NewInstExpr(t);
                    }
                    else
                    {
                        var staticTypeCreator = t.PolymorphicClassIdOwner().RealName(true);
                        if (t.IsGenericParameter)
                        {
                            if (t.GetGenericParameterConstraints().TryFind(par => !par.IsInterface, out var hardPar))
                            {
                                staticTypeCreator = hardPar.PolymorphicClassIdOwner().RealName(true);
                            }
                            else
                            {
                                Error($"can't generate new instance construction for unknown type {t} in {info.carrierType}, constrain this type with some base class like Livable");
                            }
                        }
                        newExpr = $"{staticTypeCreator}.{PolymorphInstanceFuncName}({classIdReader})";
                    }
                }
            }
            else
            {
                if (t.IsAbstract)
                {
                    Error($"Type {t} is abstract but required to have constructor during {sink.classBuilder.name} generation");
                    return;
                }
                newExpr = NewInstExpr(t, info.defaultValue);
            }

            sink.content($"{(needCreateVar ? "var " : "")}{name} = {(needCast ? $"({t.RealName(true)})" : "")}{newExpr};");
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

            t.ProcessMembers(GenTaskFlags.DefaultConstructor, false, info =>
            {
                if (info.type.IsValueType && info.isValueWrapper == ValueVrapperType.None) return;
                if (info.type.IsEnum && info.isValueWrapper == ValueVrapperType.None) return;
                if (info.type.IsConfig() && info.isValueWrapper == ValueVrapperType.None && info.insideConfigStorage == false) return;
                
                // Livable slot can be readonly and kind of can be null at the same time due to value transformer.
                if (info.canBeNull && info.isReadOnly && info.isValueWrapper == ValueVrapperType.None)
                {
                    Error($"{info} in type {t} can't be marked readonly and have CanBeNull tag at the same time");
                    return;
                }

                if (info.justData == false && !info.isReadOnly && info.type.IsLivableCustomType())
                {
                    Error($"{info} in type {t} is livable and can be presented only as readonly field, If you want to change this field runtime use LivableSlot, may be you need to use [JustData] attribute");
                    return;
                }
                if (info.isValueWrapper == ValueVrapperType.None && info.canBeNull) return;
                //if (info.type.IsLivableContainer()) return;
                // For livables all configs should be set in Prepare method thats why its unnesseseary to generate default config values
                if (info.isValueWrapper == ValueVrapperType.None && info.type.IsLoadableConfig() && t.IsLivableCustomType()) return;
                CreateNewInstance(constructor, info, null, null, false, wrapType: true);
                InitializeWrappedValues(constructor, info);
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

        static void InitializeWrappedValues(MethodBuilder constructor, ZRData info)
        {
            if (info.WrapperTypes.Count == 0) return;

            var declaredType = info.realType;
            var access = info.realAccess;
            for (var i = 0; i < info.WrapperTypes.Count; ++i)
            {
                var wrapper = info.WrapperTypes[i];
                if (wrapper is not (FieldWrapperType.Cell or FieldWrapperType.LivableSlot)) continue;

                var innerType = declaredType?.FirstGenericArg();
                if (innerType == null) return;

                var nextWrapper = i + 1 < info.WrapperTypes.Count
                    ? info.WrapperTypes[i + 1]
                    : FieldWrapperType.None;
                if (nextWrapper is FieldWrapperType.Cell or FieldWrapperType.LivableSlot)
                {
                    constructor.content($"{access}.value = {innerType.NewInstExpr()};");
                    access += ".value";
                    declaredType = innerType;
                    continue;
                }

                if (nextWrapper == FieldWrapperType.Nullable) return;
                if (!info.canBeNull && !info.type.IsValueType)
                {
                    constructor.content($"{access}.value = {info.type.NewInstExpr()};");
                }
                return;
            }
        }
    }
}
