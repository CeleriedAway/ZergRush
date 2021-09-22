using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ZergRush.Alive;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string SetupChildrenIdFuncName = "__GenIds";
        public static string SetupHierarchyFuncName = "__PropagateHierarchyAndRememberIds";
        public static string LeaveHierarchyFuncName = "__ForgetIds";
        
        static void GenerateHierarchyAndId(Type type, string funcPrefix)
        {
            if (type.HasAttribute<HasRefId>())
            {
                var classSink = GenClassSink(type);
                var checkAlive = ""; //type.IsLivableGen() ? "if (alive) " : "";
                classSink.content("public int Id { get { return id; } set { id = value; " + checkAlive +
                                  "root?.ForceId(value, this); } }");
                classSink.inheritance(typeof(IReferencableFromDataRoot).Name);
            }

            // Auto id generation on creation
            var setupIds = MakeGenMethod(type, GenTaskFlags.OwnershipHierarchy, SetupChildrenIdFuncName, Void, $"DataRoot __root");
            if (type.HasAttribute<HasRefId>())
                setupIds.content($"Id = __root.entityIdFactory++;");
            type.ProcessMembers(GenTaskFlags.OwnershipHierarchy, false, info =>
            {
                if (info.isValueWrapper == ValueVrapperType.None && info.type.IsDataNode())
                {
//                    if (info.type.HasReferenceId())
//                        setupIds.content($"{info.access}.Id = root.entityIdFactory++;");
                    if (info.type.HasNestedLivableChildren() || info.type.HasReferenceId())
                        setupIds.content($"{info.access}.{SetupChildrenIdFuncName}(__root);");
                }
                else if (info.type.IsLivableList())
                {
                    setupIds.content($"{info.access}.{SetupChildrenIdFuncName}(__root);");
                }
            });

            // Hierarchy propagation
            var setupHierarchy =
                MakeGenMethod(type, GenTaskFlags.OwnershipHierarchy, SetupHierarchyFuncName, Void, $"");
            
            TraverseGenCustomType(new TraversStrategy
            {
                flag = GenTaskFlags.OwnershipHierarchy,
                funcName = LeaveHierarchyFuncName,
                needDictKeyTraverse = false,
                interfaceType = null,
                needMembersGenRequest = false,
                memberPredicate = info => !info.justData && (info.type.IsDataNode() || info.type.IsDataList() || info.type.IsLivableSlot()),
                start = (sink, hasBaseCall) => {
                    if (type.HasAttribute<HasRefId>())
                    {
                        sink.content($"if (Id != 0) root.Forget(Id, this);");
                    }
                },
                elemProcess = (sink, info) => {
                    sink.content($"{info.baseAccess}.{LeaveHierarchyFuncName}();");
                },
                finish = sink => {},
                funcReturnType = Void
            }, type, funcPrefix);
            
            if (type.HasAttribute<HasRefId>())
            {
                setupHierarchy.content($"if (Id != 0) root.Remember(this, Id);");
            }
            
            type.ProcessMembers(GenTaskFlags.OwnershipHierarchy, false, info =>
            {
                if (info.justData) return;
                if (info.type.IsRootNeededEvent())
                {
                    setupHierarchy.content($"{info.access}.root = root;");
                }

                if (info.realType.NeedsHierarchy())
                {
                    setupHierarchy.content($"{info.baseAccess}.root = root;");
                    setupHierarchy.content($"{info.baseAccess}.carrier = this;");
                    //if (info.type.IsLivableContainer() || info.isValueWrapper == ValueVrapperType.LivableSlot || info.type.HasChildrenThatNeedsRootSetup())
                    //if (info.type.HasChildrenThatNeedsRootSetup() || info.type.IsLivableList() || info.type.IsLivableSlot())
                    setupHierarchy.content($"{info.baseAccess}.{SetupHierarchyFuncName}();");
                }
            });
            
        }
        
        static void GenerateConstructionFromRoot(Type type)
        {
            var rootType = type.FindTagInHierarchy<RootType>()?.type;
            if (rootType == null) return;
            
            Action<MethodBuilder> fillCreateWithLivableSetup = sink =>
            {
                sink.content($"inst.root = this;");
//                if (type.HasReferenceId())
//                    sink.content($"inst.Id = entityIdFactory++;");
                //if (type.HasNestedLivableChildren())
                sink.content($"inst.{SetupChildrenIdFuncName}(this);");
                sink.content($"inst.{SetupHierarchyFuncName}();");
                //if (type.HasChildrenThatNeedsRootSetup())
            };
            
            var constructorMethodFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance
                            | BindingFlags.Public | BindingFlags.NonPublic;
            if (type.IsAbstract == false && (!type.IsGenericType || type.IsConstructedGenericType))
            {
                bool hasConstructor = false;
                foreach (var methodInfo in type.GetMethods(constructorMethodFlags))
                {
                    if (methodInfo.Name.StartsWith("Ctor"))
                    {
                        var sig = methodInfo.GetParameters().Select(p => $"{p.ParameterType.RealName(true)} {p.Name}")
                            .PrintCollection();
                        var call = methodInfo.GetParameters().Select(p => p.Name).PrintCollection();
                        // ctor mwthod found
                        var constructFull = GenClassSink(rootType).Method(type.CreateLivableInRootFunc(), rootType, MethodType.Instance, type, sig, "", "");
                        constructFull.indent++;
                        //constructFull.content($"var inst = pool.{type.GetFromPoolFunc()}();");
                        CreateNewInstance(constructFull, new DataInfo{type = type, baseAccess = "inst", sureIsNull = true}, "", true, "", true );
                        fillCreateWithLivableSetup(constructFull);
                        
                        constructFull.content($"inst.{methodInfo.Name}({call});");
                        constructFull.content($"return inst;");
                        hasConstructor = true;
                    }
                }

                if (!hasConstructor)
                {
                    var createWithSetup = GenClassSink(rootType).Method(type.CreateLivableInRootFunc(), rootType, MethodType.Instance, type, "", "", "");
                    createWithSetup.indent++;
                    CreateNewInstance(createWithSetup, new DataInfo{type = type, baseAccess = "inst", sureIsNull = true}, "", true, "", true );
                    fillCreateWithLivableSetup(createWithSetup);
                    createWithSetup.content($"return inst;");
                }

            }
            
            // Create from prototype
            if ((type.ReadGenFlags() & GenTaskFlags.UpdateFrom) != 0 && type.IsGenericTypeDecl() == false)
            {
                var createFromProrotype = GenClassSink(rootType).Method(type.CreateLivableInRootFunc(), rootType, MethodType.Instance, type, $"{type.RealName(true)} prototype", "", "");
                createFromProrotype.indent++;
                //CreateNewInstance(createFromProrotype, new DataInfo{type = type, baseAccess = "inst", sureIsNull = true}, "", true, "", true );
                //createFromProrotype.content($"var inst = ({type.RealName(true)})prototype.NewInst();");
                GenUpdateValueFromInstance(createFromProrotype, new DataInfo {type = type, baseAccess = $"inst", sureIsNull = true}, "prototype", false, needCreateVar: true);
                fillCreateWithLivableSetup(createFromProrotype);
                createFromProrotype.content($"return inst;");
            }

            if (polymorphicRootNodes.ContainsKey(type))
            {
                foreach (var methodInfo in type.GetMethods(constructorMethodFlags))
                {
                    if (methodInfo.Name.StartsWith("Ctor") && (methodInfo.IsVirtual || methodInfo.IsAbstract))
                    {
                        var sig = methodInfo.GetParameters().Select(p => $"{p.ParameterType.RealName(true)} {p.Name}")
                            .PrintCollection();
                        var enumTypeRef = type.PolymorphicRootTypeEnumName();
                        if (string.IsNullOrEmpty(type.Namespace) == false)
                            enumTypeRef = type.Namespace + "." + enumTypeRef;
                        sig = CodeGenTools.MergeSig($"{enumTypeRef} classId", sig);
                        var call = methodInfo.GetParameters().Select(p => p.Name).PrintCollection();
                        // ctor mwthod found
                        var constructFull = GenClassSink(rootType).Method($"CreatePolymorphic{type.UniqueName(false)}", 
                            rootType, MethodType.Instance, type, sig, "", "");
                        constructFull.indent++;
                        constructFull.content($"var inst = {type.NewPolymorphicFromClassIdExpression(type.NeedsPooledPolymorphConstruction())};");
                        fillCreateWithLivableSetup(constructFull);                       
                        constructFull.content($"inst.{methodInfo.Name}({call});");
                        constructFull.content($"return inst;");
                    }
                }
            }
            
        }
        
        static bool IsHierarchySupportContainer(this Type t)
        {
            return t.IsLivableContainer() || t.IsConstructedGenericType &&
                   (t.IsGenericOfType(typeof(Ref<>)) ||
                    t.IsGenericOfType(typeof(__OldRefList<>)) ||
                    t.IsGenericOfType(typeof(RefListMk2<>)) ||
                    t.IsGenericOfType(typeof(DataList<>))
                    );
        }
        
        static bool IsDataList(this Type t)
        {
            return t.IsLivableList() || t.IsConstructedGenericType && t.IsGenericOfType(typeof(DataList<>));
        }
        public static bool IsDataNode(this Type t)
        {
            return typeof(DataNode).IsAssignableFrom(t);
        }
        static bool IsReferencableDataNode(this Type t)
        {
            return t.IsDataNode() && typeof(IReferencableFromDataRoot).IsAssignableFrom(t);
        }
        public static bool IsDataRoot(this Type t)
        {
            return typeof(DataRoot).IsAssignableFrom(t);
        }
        static bool NeedsHierarchy(this Type t)
        {
            return t.IsDataNode() || t.IsHierarchySupportContainer();
        }
        static bool IsRef(this Type t)
        {
            return t.IsConstructedGenericType &&
                   t.IsGenericOfType(typeof(Ref<>));
        }
        static bool HasChildrenThatNeedsRootSetup(this Type t)
        {
            return t.GetMembersForCodeGen(GenTaskFlags.LifeSupport, true)
                .Any(v => v.type.IsLivableCustomType() || v.type.IsRootNeededEvent() || v.type.IsLivableContainer());
        }

        static bool IsOneOfThose(this string self, params string[] strs)
        {
            return strs.Any(str => self == str);
        }
        
        static bool IsRootNeededEvent(this Type type)
        {
            return
                false; // type.IsGenericOfType(typeof(EventBuffer<>)) || type.IsGenericOfType(typeof(LCompositeEvent<>));
        }
        
        public static bool HasReferenceId(this Type type)
        {
            return type.ParentWithTag<HasRefId>() != null;
        }

    }
}