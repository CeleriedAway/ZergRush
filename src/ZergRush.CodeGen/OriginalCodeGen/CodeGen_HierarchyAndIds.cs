using System;
using Type = ZergRush.CodeGen.ZRType;
using System.Linq;
using System.Reflection;
using ZergRush.Alive;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string SetupHierarchyFuncName = "__PropagateHierarchy";
        
        static void GenerateHierarchyAndId(Type type, string funcPrefix)
        {
            // Hierarchy propagation
            var setupHierarchy =
                MakeGenMethod(type, GenTaskFlags.OwnershipHierarchy, SetupHierarchyFuncName, Void, $"");
            
            type.ProcessMembers(GenTaskFlags.OwnershipHierarchy, false, (member, info, declaredAccess) =>
            {
                if (info.JustData) return;
                if (info.Type.IsRootNeededEvent())
                {
                    setupHierarchy.content($"{info.Access}.root = root;");
                }

                if ((member.DeclaredType ?? info.Type).NeedsHierarchy())
                {
                    setupHierarchy.content($"{declaredAccess}.{nameof(Livable.SetRootAndCarrier)}(root, this);");
                    setupHierarchy.content($"{declaredAccess}.{SetupHierarchyFuncName}();");
                }
            }, GenericMembers(setupHierarchy));
            
        }
        
        static void GenerateConstructionFromRoot(Type type)
        {
            var rootType = type.FindTagInHierarchy<RootType>()?.type;
            if (rootType == null) return;
            
            Action<MethodBuilder> fillCreateWithLivableSetup = sink =>
            {
                sink.content($"inst.root = this;");
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
                            .ToCodeGenListString();
                        var call = methodInfo.GetParameters().Select(p => p.Name).ToCodeGenListString();
                        // ctor mwthod found
                        var constructFull = GenClassSink(rootType).Method(type.CreateLivableInRootFunc(), rootType, MethodType.Instance, type, sig, "", "");
                        constructFull.indent++;
                        CreateNewInstance(constructFull, type, "inst", "", "", true, carrierType: rootType);
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
                    CreateNewInstance(createWithSetup, type, "inst", "", "", true, carrierType: rootType);
                    fillCreateWithLivableSetup(createWithSetup);
                    createWithSetup.content($"return inst;");
                }

            }
            
            // Create from prototype
            if ((type.ReadGenFlags() & GenTaskFlags.UpdateFrom) != 0)
            {
                var createFromProrotype = GenClassSink(rootType).Method(type.CreateLivableInRootFunc(), rootType, MethodType.Instance, type, $"{type.RealName(true)} prototype", "", "");
                createFromProrotype.indent++;
                //createFromProrotype.content($"var inst = ({type.RealName(true)})prototype.NewInst();");
                
                createFromProrotype.content($"var {HelperName} = new {UpdateFromHelperClassName}();");
                GenUpdateValueFromInstance(createFromProrotype,
                    new ZRData("inst", type, ZRDataOption.SureIsNull), type, "inst", rootType,
                    "prototype", false, needCreateVar: true, supportMultiRef: false);
                fillCreateWithLivableSetup(createFromProrotype);
                createFromProrotype.content($"return inst;");
            }

            if (type.IsPolymorphicConstructionRoot())
            {
                foreach (var methodInfo in type.GetMethods(constructorMethodFlags))
                {
                    if (methodInfo.Name.StartsWith("Ctor") && (methodInfo.IsVirtual || methodInfo.IsAbstract))
                    {
                        var sig = methodInfo.GetParameters().Select(p => $"{p.ParameterType.RealName(true)} {p.Name}")
                            .ToCodeGenListString();
                        var enumTypeRef = type.PolymorphicRootTypeEnumName();
                        if (string.IsNullOrEmpty(type.Namespace) == false)
                            enumTypeRef = type.Namespace + "." + enumTypeRef;
                        sig = CodeGenTools.MergeSig($"{enumTypeRef} {CodeGenImplTools.ClassIdName}", sig);
                        var call = methodInfo.GetParameters().Select(p => p.Name).ToCodeGenListString();
                        // ctor mwthod found
                        var constructFull = GenClassSink(rootType).Method($"CreatePolymorphic{type.UniqueName(false)}", 
                            rootType, MethodType.Instance, type, sig, "", "");
                        constructFull.indent++;
                        constructFull.content($"var inst = {type.NewPolymorphicFromClassIdExpression()};");
                        fillCreateWithLivableSetup(constructFull);                       
                        constructFull.content($"inst.{methodInfo.Name}({call});");
                        constructFull.content($"return inst;");
                    }
                }
            }
            
        }
        
        static bool IsHierarchySupportContainer(this Type t)
        {
            return t.IsLivableContainer();
        }
        
        public static bool IsLivableNode(this Type t)
        {
            return !t.IsHierarchySupportContainer() && t.IsAssignableTo(typeof(Livable));
        }

        public static bool IsLivableRoot(this Type t)
        {
            return t.IsAssignableTo(typeof(LivableRoot));
        }
        static bool NeedsHierarchy(this Type t)
        {
            return t.IsLivableNode() || t.IsHierarchySupportContainer();
        }
        static bool IsRef(this Type t)
        {
            return false;
        }
        static bool HasChildrenThatNeedsRootSetup(this Type t)
        {
            return t.GetMembersForCodeGen(GenTaskFlags.LifeSupport, true)
                .Any(member => member.MemberType.IsLivableCustomType() ||
                    member.MemberType.IsRootNeededEvent() || member.MemberType.IsLivableContainer());
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
            return false;
        }

    }
}
