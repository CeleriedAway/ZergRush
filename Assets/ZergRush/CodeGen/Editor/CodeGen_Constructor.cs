using System;
using ZergRush.Alive;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        static bool HasPooledUpdateFromMethod(this Type type)
        {
            return type.ReadGenFlags().HasFlag(GenTaskFlags.PooledUpdateFrom);
        }
        
        static bool HasGeneratedDefaultConstructor(this Type type)
        {
            return type.ReadGenFlags().HasFlag(GenTaskFlags.DefaultConstructor);
        }
        
        static bool HasDefaultConstructor(this Type type)
        {
            if (type == typeof(object)) return true;
            return type.ReadGenFlags().HasFlag(GenTaskFlags.DefaultConstructor) || type.GetConstructor(Type.EmptyTypes) != null;
        }
        
        static bool HasPooledDeserializeMethod(this Type type)
        {
            return type.ReadGenFlags().HasFlag(GenTaskFlags.PooledDeserialize);
        }

        static bool IsLivableAncestor(this Type type)
        {
            return typeof(Livable).IsAssignableFrom(type);
        }
        
        static string PoolTypeName(this Type type)
        {
            return "ObjectPool";
        }
        static Type PoolType(this Type type)
        {
            return typeof(ObjectPool);
        }

        static bool HasPool(this Type t)
        {
            return t.ReadGenFlags().HasFlag(GenTaskFlags.Pooled);
        }
        static string PersonalPoolName(this Type t)
        {
            return t.UniqueName() + "Pool";
        }
        
        static string GetFromPoolFunc(this Type t)
        {
            return "_Get" + t.UniqueName();
        }
        
        static string OptPoolArgDecl(this Type t, bool need)
        {
            return need && t.HasPool() ? $"{t.PoolTypeName()} pool" : "";
        }
        
        static string OptPoolArg(this Type t, bool need)
        {
            return need && t.HasPool() ? $"pool" : "";
        }
        
        static string OptPoolSecondArgDecl(this Type t, bool need)
        {
            return need && t.HasPool() ? $", {t.PoolTypeName()} pool" : "";
        }

        static bool CanPassPoolInfoUpdateFromIfNeeded(this Type t, bool need)
        {
            return need && ((t.ReadGenFlags() & GenTaskFlags.PooledUpdateFrom) != 0 ||
                t.IsLivableList() || t.IsLivableSlot() ||
                (t.IsLivableContainer() && t.FirstGenericArg().CanPassPoolInfoUpdateFromIfNeeded(need)));
        }
        
        static string OptPoolIfUpdatebleWithPoolSecondArg(this Type t, bool need)
        {
            return t.CanPassPoolInfoUpdateFromIfNeeded(need) ? $", pool" : "";
        }
        
        static string OptPoolSecondArg(this Type t, bool need)
        {
            return need && t.HasPool() ? $", pool" : "";
        }

        static bool CanBeNullAfterConstruction(this DataInfo info)
        {
            return info.type.CanBeAncestor() && info.cantBeAncestor == false;
        }
        
        public static void CreateNewInstance(MethodBuilder sink, DataInfo info, string classIdReader, bool pooled,
            string refInst, bool needCreateVar, bool wrapType = false)
        {
            // Some bullshit logic here
            // All of this because of value wrapper concept that should be reconsidered
            var t = wrapType ? info.realType : info.type;
            var name = wrapType ? info.accessPrefix + info.baseAccess : info.access;
            
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
                    string poolArg = "";
                    if (t.HasPool() && pooled)
                    {
                        poolArg = "pool";
                    }
                    newExpr = $"{refInst}.{PolymorphNewInstOfSameType}({poolArg})";
                }
                else
                {
                    if (classIdReader.Valid() == false)
                    {
                        if (t.IsAbstract) { return; }
                        newExpr = NewInstExpr(t, pooled);
                    }
                    else
                    {
                        var staticTypeCreator = baseClassMap.GetOrDefault(t, t).RealName(true);
                        if (t.IsGenericParameter)
                        {
                            if (t.GetGenericParameterConstraints().TryFind(par => !par.IsInterface, out var hardPar))
                            {
                                staticTypeCreator = hardPar.RealName(true);
                            }
                            else
                            {
                                Error($"can't generate new instance construction for unknown type {t} in {info.carrierType}, constrain this type with some base class like DataNode");
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
                newExpr = NewInstExpr(t, pooled, info.defaultValue);
            }

            sink.content($"{(needCreateVar ? "var " : "")}{name} = {(needCast ? $"({t.RealName(true)})" : "")}{newExpr};");
        }

        public static void GenerateConstructor(Type t)
        {
            if (t.IsControllable() == false)
            {
                Error($"you can't generate constructor as extension method!");
                return;
            }

            if (t.IsValueType) return;

            var constructor = MakeGenMethod(t, GenTaskFlags.DefaultConstructor, t.ClearName(), null, "");
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
                CreateNewInstance(constructor, info, null, false, null, false, wrapType: true);
            });
            
            if (t.HasAttribute<GenModelRootSetup>())
            {
                constructor.content("root = this;");
                constructor.content($"{SetupChildrenIdFuncName}(root);");
                constructor.content($"{SetupHierarchyFuncName}();");
            }
        }

        static string NewInstExpr(Type t, bool pooled, object constructorArg = null)
        {
            if (t.HasPool() && pooled)
            {
                return $"pool.{t.GetFromPoolFunc()}()";
            }
            else if (t.HasDefaultConstructor())
            {
                var arg = constructorArg != null ? (constructorArg is string ? $"\"{constructorArg}\"" : constructorArg) : "";
                return $"new {t.RealName(true)}({arg})";
            }
            else
            {
                return $"default({t.RealName(true)})";
            }
        }

        public static void SinkRemovePostProcess(MethodBuilder sink, DataInfo info, bool pooled)
        {
//            if (info.type.HasPool() && pooled)
//            {
//                sink.content($"{info.access}?.{PolymorphReturnToPool}(pool);");
//            }
        }
    }
}