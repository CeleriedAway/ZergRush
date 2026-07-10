using System;
using Type = ZergRush.CodeGen.ZRType;
using System.Linq;
using ZergRush.Alive;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static string LivableEntryEnliveName = "Enlive";
        public static string LivableEntryMortifyName = "Mortify";

        public static string LivableGeneratedEnliveName = "Enlive";
        public static string LivableGeneratedMortifyName = "Mortify";

        public static string LivableGeneratedEnliveChildrenName = "EnliveChildren";
        public static string LivableGeneratedMortifyChildrenName = "MortifyChildren";

        public static string LivableCustomEnliveName = "EnliveSelf";
        public static string LivableCustomMortifyName = "MortifySelf";

        public static string LivableEnliveArgs = "";//"";
        public static string LivableEnliveCallArgs = "";//"";

        static bool IsLivableCustomType(this Type t)
        {
            return t.IsAssignableTo(typeof(Livable)) && !t.IsLivableContainer();
        }

        static bool IsLivableContainer(this Type t)
        {
            return t.IsConstructedGenericType &&
                   (t.IsGenericOfType(typeof(LivableList<>)) ||
                    t.IsGenericOfType(typeof(LivableSlot<>))
                    );
        }
        static bool IsLivableGen(this Type t)
        {
            return ((t.ReadGenFlags() & GenTaskFlags.LifeSupport) != 0) || t.IsLivableContainer() || (t.IsLivableAncestor());
        }
        static bool IsLivableList(this Type t)
        {
            return t.IsGenericOfType(typeof(LivableList<>));
        }
        public static bool IsLivableSlot(this Type t)
        {
            if (t == null) return false;
            var tName = t.Name;
            return tName.StartsWith("LivableSlot");
        }
        static bool HasNestedLivableChildren(this Type t)
        {
            return t.GetMembersForCodeGen(GenTaskFlags.LifeSupport, true)
                .Any(member => member.IsReadOnly && member.MemberType.IsLivableCustomType() ||
                    member.MemberType.IsLivableList());
        }

        static void GenerateLivable(Type type, string funcPrefix)
        {
            if (type.IsLivableGen() == false)
            {
                Error($"Type {type.RealName(true)} must be Livable ancestor to generate life support system");
                return;
            }
            
            var sinkEnlive = MakeGenMethod(type, GenTaskFlags.LifeSupport, funcPrefix + LivableGeneratedEnliveName, Void,
                LivableEnliveArgs);
            sinkEnlive.doNotCallBaseMethod = true;
            var sinkMortify = MakeGenMethod(type, GenTaskFlags.LifeSupport, funcPrefix + LivableGeneratedMortifyName, Void,
                "");
            sinkMortify.doNotCallBaseMethod = true;
            
            sinkEnlive.classBuilder.usingSink("ZergRush.Alive");
            sinkEnlive.classBuilder.usingSink("ZergRush");
            
            sinkEnlive.content($"{LivableCustomEnliveName}({LivableEnliveCallArgs});");
            sinkMortify.content($"{LivableCustomMortifyName}();");
            
            sinkEnlive.content($"{LivableGeneratedEnliveChildrenName}({LivableEnliveCallArgs});");
            sinkMortify.content($"{LivableGeneratedMortifyChildrenName}();");
            
            var sinkEnliveChildren = MakeGenMethod(type, GenTaskFlags.LifeSupport, funcPrefix + LivableGeneratedEnliveChildrenName, Void, LivableEnliveArgs);
            sinkEnliveChildren.access = MethodAccess.Protected;
            var sinkMortifyChildren = MakeGenMethod(type, GenTaskFlags.LifeSupport, funcPrefix + LivableGeneratedMortifyChildrenName, Void, "");
            sinkMortifyChildren.access = MethodAccess.Protected;
            
            type.ProcessMembers(GenTaskFlags.LifeSupport, false, (member, info, declaredAccess) =>
            {
                if (info.JustData) return;
                if ((info.Type.IsArray || info.Type.IsList()) && !info.Type.IsHierarchySupportContainer() && info.Type.FirstGenericArg().IsLivableGen())
                {
                    Error($"field {info.Access} in type {type} is list of livable values which is not allowed. " +
                          $"Use LivableList to store livable values");
                    return;
                }
                if (member.WrapperTypes.FirstOrDefault() == FieldWrapperType.Cell && info.Type.IsLivableGen())
                {
                    Error($"field {info.Access} in type {type} is cell of livable value which is not allowed. " +
                          $"Use LivableSlot to dynamically store livable value");
                    return;
                }
                if (!info.Type.IsLivableGen()) return;
                if (member.WrapperTypes.Count == 0 && info.Type.CanBeAncestor() && !info.CantBeAncestor)
                {
                    Error($"field {info.Access} in type {type} is polymorphic and readonly livable, " +
                          $"use [CantBeAscestor] tag to guarantee its type");
                    return;
                }
                
                sinkEnliveChildren.content($"{declaredAccess}.{LivableEntryEnliveName}();");
                sinkMortifyChildren.content($"{declaredAccess}.{LivableEntryMortifyName}();");
            }, GenericMembers(sinkEnliveChildren, sinkMortifyChildren));

            TraverseGenCustomType(new TraversStrategy
            {
                funcName = "VisitNode",
                funcArgs = "Action<object> action",
                interfaceType = null,
                needMembersGenRequest = false,
                memberPredicate = (_, info) => !info.JustData &&
                    (info.Type.IsLivableNode() || info.Type.IsLivableContainer() || info.Type.IsLivableList()),
                memberProcess = (sink, _, _, declaredAccess) =>
                    sink.content($"{declaredAccess}.VisitNode(action);"),
                elemProcess = (sink, info) => sink.content($"{info.Access}.VisitNode(action);"),
                flag = GenTaskFlags.OwnershipHierarchy
            }, type, funcPrefix);

        }
        
        static string CreateLivableInRootFunc(this Type t)
        {
            return "Create" + t.UniqueName(false);
        }
    }
}
