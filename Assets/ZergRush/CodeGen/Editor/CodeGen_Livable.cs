using System;
using System.Linq;
using ZergRush.Alive;
using ZergRush.CodeGen;

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

        public static Type RefIdType = typeof(int);

        static bool IsLivableCustomType(this Type t)
        {
            return typeof(Livable).IsAssignableFrom(t) && !t.IsLivableContainer();
        }

        static bool IsLivableContainer(this Type t)
        {
            return t.IsConstructedGenericType &&
                   (t.IsGenericOfType(typeof(LivableList<>)) ||
                    t.IsGenericOfType(typeof(ModifiableLivableList<>)) ||
                    t.IsGenericOfType(typeof(LivableSlot<>))
                    );
        }
        static bool IsLivableGen(this Type t)
        {
            return ((t.ReadGenFlags() & GenTaskFlags.LifeSupport) != 0) || t.IsLivableContainer() || (t.IsLivableAncestor());
        }
        static bool IsModifiableLivableList(this Type t)
        {
            return t.IsConstructedGenericType && (t.IsGenericOfType(typeof(ModifiableLivableList<>)));
        }
        static bool IsLivableList(this Type t)
        {
            return t.IsConstructedGenericType &&
                   (t.IsGenericOfType(typeof(LivableList<>)) || t.IsGenericOfType(typeof(ModifiableLivableList<>)));
        }
        static bool IsLivableSlot(this Type t)
        {
            if (t == null) return false;
            var tName = t.Name;
            return tName.StartsWith("LivableSlot") || t.Name.StartsWith("DataSlot");
            // return t.IsConstructedGenericType && t.IsGenericOfType(typeof(LivableSlot<>)) ||
            //     t.IsConstructedGenericType && t.IsGenericOfType(typeof(DataSlot<>));
        }
        static bool HasNestedLivableChildren(this Type t)
        {
            return t.GetMembersForCodeGen(GenTaskFlags.LifeSupport, true)
                .Any(v => v.isReadOnly && v.type.IsLivableCustomType() || v.type.IsLivableList());
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
            
            //string rootName = "root";

//            if (type.HasInHierarchy(t => t.HasAttribute<HasRefId>()))
//            {
//                sinkEnlive.content($"if (Id != 0) root.Remember(this, Id);");
//                sinkMortify.content($"{rootName}.Forget(Id, this);");
//            }
            
            sinkEnlive.content($"{LivableCustomEnliveName}({LivableEnliveCallArgs});");
            sinkMortify.content($"{LivableCustomMortifyName}();");
            
            sinkEnlive.content($"{LivableGeneratedEnliveChildrenName}({LivableEnliveCallArgs});");
            sinkMortify.content($"{LivableGeneratedMortifyChildrenName}();");
            
            var sinkEnliveChildren = MakeGenMethod(type, GenTaskFlags.LifeSupport, funcPrefix + LivableGeneratedEnliveChildrenName, Void, LivableEnliveArgs);
            sinkEnliveChildren.access = MethodAccess.Protected;
            var sinkMortifyChildren = MakeGenMethod(type, GenTaskFlags.LifeSupport, funcPrefix + LivableGeneratedMortifyChildrenName, Void, "");
            sinkMortifyChildren.access = MethodAccess.Protected;
            
            type.ProcessMembers(GenTaskFlags.LifeSupport, false, info =>
            {
                if (info.justData) return;
                if ((info.type.IsArray || info.type.IsList()) && !info.type.IsHierarchySupportContainer() && info.type.FirstGenericArg().IsLivableGen())
                {
                    Error($"field {info.access} in type {type} is list of livable values which is not allowed. " +
                          $"Use LivableList to store livable values");
                    return;
                }
                if (info.isValueWrapper == ValueVrapperType.Cell && info.type.IsLivableGen())
                {
                    Error($"field {info.access} in type {type} is cell of livable value which is not allowed. " +
                          $"Use LivableSlot to dynamically store livable value");
                    return;
                }
                if (info.type.IsLivableGen() == false) return;
                if (info.isValueWrapper == ValueVrapperType.None && info.type.CanBeAncestor() && info.cantBeAncestor == false)
                {
                    Error($"field {info.access} in type {type} is polymorphic and readonly livable, " +
                          $"use [CantBeAscestor] tag to guarantee its type");
                    return;
                }
                
                sinkEnliveChildren.content($"{info.baseAccess}.{LivableEntryEnliveName}();");
                sinkMortifyChildren.content($"{info.baseAccess}.{LivableEntryMortifyName}();");
            });
            
        }
        
        static string CreateLivableInRootFunc(this Type t)
        {
            return "Create" + t.UniqueName(false);
        }
    }
}