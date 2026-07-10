using System;
using Type = ZergRush.CodeGen.ZRType;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ZergRush.Alive;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public const bool UseClassIdCaching = false;

        public const string PolymorphClassIdTypeName = "ushort";
        public static readonly Type PolymorphClassIdType = typeof(ushort);
        public static readonly string PolymorphClassIdFunc = "GetClassId";
#if UseClassIdCaching
        public static readonly string PolymorphClassIdGetter = "ClassIdCached()";
#else
        public static readonly string PolymorphClassIdGetter = "GetClassId()";
#endif
        public static readonly string PolymorphClassIdGetterName = "ClassIdCached";
        public static readonly string PolymorphClassIdCached = "__classId";
        public static readonly string PolymorphInstanceFuncName = "CreatePolymorphic";
        
        public static readonly string PolymorphNewInstOfSameType = "NewInst";
        const string TypeEnumName = "Types";

        static Dictionary<Type, HashSet<Type>> genericInstances = new Dictionary<Type, HashSet<Type>>();

        public static string PolymorphicRootTypeEnumName(this Type t)
        {
            return t.UniqueName(false) + "Type";
        }

        public static bool CanBeAncestor(this Type t)
        {
            return !t.IsSealed && t.ChildTypes.Count > 0 && t.PolymorphicConstructionRoot() != null;
        }

        static Type PolymorphicConstructionRoot(this Type t)
        {
            const GenTaskFlags flag = GenTaskFlags.PolymorphicConstruction;
            if ((t.ReadGenFlags() & flag) == 0) return null;

            Type root = null;
            for (var current = t; current != null; current = current.BaseType)
            {
                if ((current.ReadGenFlags() & flag) != 0 && (current.Flags & flag) != 0)
                {
                    root = current;
                }
            }

            return root;
        }

        static bool IsPolymorphicConstructionRoot(this Type t)
        {
            return t.PolymorphicConstructionRoot() == t;
        }

        static Type PolymorphicClassIdOwner(this Type t)
        {
            return t.PolymorphicConstructionRoot() ?? t;
        }

        static string NewPolymorphicFromClassIdExpression(this Type type)
        {
            return
                $"({type.RealName(true)}){type.RealName(true)}.{PolymorphInstanceFuncName}(({PolymorphClassIdTypeName}) " +
                $"{CodeGenImplTools.ClassIdName})";
        }

        static bool IsValidType(this Type t)
        {
            return t.IsGenericType == false || (t.IsConstructedGenericType &&
                                                t.GetGenericArguments().All(a => a.IsGenericParameter == false));
        }


        static bool IsGenericTypeDecl(this Type t)
        {
            return t.IsGenericType && t.GetGenericArguments().All(a => a.IsGenericParameter);
        }

        static void PrintGenericSwitch(this Type genericDef, MethodBuilder sink,
            Action<Type, MethodBuilder> codeForType)
        {
            //TODO implement for multiple generic args
            var T = genericDef.GetGenericArguments()[0];
            if (genericDef.IsConstructedGenericType)
            {
                genericDef = genericDef.GetGenericTypeDefinition();
            }

            if (genericInstances.ContainsKey(genericDef))
            {
                bool first = true;
                foreach (var type in genericInstances[genericDef]
                             .OrderBy(type => type.FirstGenericArg().FullName, StringComparer.Ordinal))
                {
                    sink.content(
                        $"{(first ? "" : "else ")} if (typeof({T.Name}) == typeof({type.FirstGenericArg()})) {{");
                    sink.indent++;
                    codeForType(type, sink);
                    sink.indent--;
                    sink.content($"}}");
                    first = false;
                }
            }
        }

        static string TypeTableFileName(this Type t)
        {
            return Path.Combine($"{GetContext(t).pathToSharp}", $"types_cache_{t.Name}.txt");
        }

        static void AddMultiRefInterfaces()
        {
            typeRequestMap.Keys.ForEach(t =>
            {
                if (t.IsMultipleReference()) GenClassSink(t).inheritance(nameof(IsMultiRef));
            });
        }

        static void GeneratePolymorphismSupport()
        {
            var groups = allTypesInAssemblies
                .Concat(typeGenRequested.Keys)
                .Concat(genericInstances.Values.SelectMany(types => types))
                .Where(type => type != null)
                .Distinct()
                .Select(type => (type, root: type.PolymorphicConstructionRoot()))
                .Where(pair => pair.root != null)
                .GroupBy(pair => pair.root, pair => pair.type)
                .OrderBy(group => group.Key.Namespace)
                .ThenBy(group => group.Key.Name);

            foreach (var group in groups)
            {
                var baseClass = group.Key;
                var sink = GenClassSink(baseClass);
                var polymorphicTypes = new[] { baseClass }
                    .Concat(group.Where(type => type != baseClass)
                        .OrderBy(type => type.Namespace)
                        .ThenBy(type => type.Name))
                    .Distinct()
                    .ToList();
                var constructableTypes = polymorphicTypes.Where(type => type.IsValidType()).ToList();
                var concreteTypes = constructableTypes.Where(type => !type.IsAbstract).ToList();
                var typeNames = concreteTypes.Select(type => type.UniqueName(false)).ToList();
                var fileName = baseClass.TypeTableFileName();
                var typeTable = EnumTable.Load(fileName);
                typeTable.UpdateWithNewTypes(typeNames);

                var indexedTypes = new List<Type>();
                foreach (var type in concreteTypes)
                {
                    var index = typeTable.records[type.UniqueName(false)];
                    indexedTypes.EnsureSizeWithNulls(index + 1);
                    indexedTypes[index] = type;
                }

                EnumTable.PrintEnum(sink, TypeEnumName, typeNames, type => typeTable.records[type]);
                GenClassIdFuncs(baseClass, polymorphicTypes, sink);
                GenPolymorphicRootSetup(baseClass, sink, indexedTypes);
                GenPolymorphMaps(baseClass, polymorphicTypes, sink);

                var rootEnumName = baseClass.PolymorphicRootTypeEnumName();
                var module = sink.module;
                var c = new GeneratorContext(new GenInfo {sharpGenPath = module.path});
                contexts.Add($"polymorphic:{baseClass.FullName}", c);
                module = c.createSharpCustomModule($"{baseClass.UniqueName()}Type", "enum");
                module.content("");
                if (!string.IsNullOrEmpty(sink.namespaceName))
                {
                    module.content($"namespace {sink.namespaceName} {{");
                    module.indent++;
                }

                EnumTable.PrintEnum(module, rootEnumName, typeNames,
                    type => typeTable.records[type]);
                if (!string.IsNullOrEmpty(sink.namespaceName))
                {
                    module.indent--;
                    module.content($"}}");
                }

                module.content("");

                var creatorFunc = sink.Method(PolymorphInstanceFuncName, baseClass,
                    MethodType.StaticFunction, baseClass,
                    $"{rootEnumName} {CodeGenImplTools.ClassIdName}", "", "");
                creatorFunc.content($"return {baseClass.NewPolymorphicFromClassIdExpression()};");
                sink.content(
                    $"public {rootEnumName} type => ({rootEnumName}) GetClassId();");

                var cacheDirectory = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(cacheDirectory) && !Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }
                EnumTable.SaveEnumCache(fileName, typeTable);
            }
        }

        static void GenClassIdFuncs(Type baseClass, List<Type> polymorphicTypes, SharpClassBuilder sink)
        {
            foreach (var type in polymorphicTypes)
            {
                if ((type.ReadGenCustomFlags() & GenTaskFlags.PolymorphicConstruction) != 0)
                {
                    continue;
                }

                var tSink = GenClassSink(type);
                tSink.inheritance("IPolymorphable");
                if (type.IsAbstract)
                {
                    if (type == baseClass)
                    {
                        tSink.content(
                            $"public virtual {PolymorphClassIdType} {PolymorphClassIdFunc}(){{throw new NotImplementedException();}}");
                    }
                }
                else
                {
                    var mType = type == baseClass ? MethodType.Virtual : MethodType.Override;
                    var classIdGetter = tSink.Method(PolymorphClassIdFunc, type, mType, PolymorphClassIdType, "");
                    classIdGetter.doNotCallBaseMethod = true;
                    if (type.IsGenericTypeDecl())
                    {
                        PrintGenericSwitch(type, classIdGetter,
                            (t, s) => s.content(
                                $"return ({PolymorphClassIdType}){TypeEnumName}.{t.UniqueName(false)};"));
                        classIdGetter.content("return 0;");
                    }
                    else
                    {
                        classIdGetter.content(
                            $"return ({PolymorphClassIdType}){TypeEnumName}.{type.UniqueName(false)};");
                    }
                }

                #if UseClassIdCaching
                sink.content($"[GenIgnore] public {PolymorphClassIdTypeName} {PolymorphClassIdCached};");
                var cachedGetter = sink.Method(PolymorphClassIdGetterName, baseClass, MethodType.Instance,
                    PolymorphClassIdType, "");
                cachedGetter.content(
                    $"return {PolymorphClassIdCached} == 0 ? {PolymorphClassIdCached} = {PolymorphClassIdFunc}() : {PolymorphClassIdCached};");
                #endif
            }
        }

        static void GenPolymorphicRootSetup(Type baseClass, SharpClassBuilder sink,
            List<Type> typeIndexer)
        {
            // Array with constructors
            var constructorsArrayName = "polymorphConstructors";
            sink.content(
                $"static Func<{baseClass.RealName()}> [] {constructorsArrayName} =" +
                $" new Func<{baseClass.RealName()}> [] {{");
            sink.indent++;
            for (var i = 0; i < typeIndexer.Count; i++)
            {
                var type = typeIndexer[i];
                sink.content(
                    $"() => {(type != null ? NewInstExpr(type) : "null")}, // {i}");
            }

            sink.indent--;
            sink.content($"}};");

            // Create function
            sink.content(
                $"public static {baseClass.RealName()} {PolymorphInstanceFuncName}(" +
                $"{PolymorphClassIdType} typeId) {{");
            sink.content($"\treturn {constructorsArrayName}[typeId]();");
            sink.content($"}}");
        }

        static void GenPolymorphMaps(Type baseClass, List<Type> polymorphicTypes, SharpClassBuilder sink)
        {
            // Class id overloaded functions
            foreach (var type in polymorphicTypes)
            {
                if ((type.ReadGenCustomFlags() & GenTaskFlags.PolymorphicConstruction) != 0)
                {
                    continue;
                }

                var tSink = GenClassSink(type);

                var mType = type == baseClass ? MethodType.Virtual : MethodType.Override;

                tSink.inheritance("ICloneInst");
                var newInstOfSameType = tSink.Method(PolymorphNewInstOfSameType, type, mType, typeof(object),
                    "");

                newInstOfSameType.doNotCallBaseMethod = true;

                if (type.Name == "Livable")
                {
                    // a hack required to make good application template
                    newInstOfSameType.doNotGen = true;
                }
                else if (type.IsAbstract) newInstOfSameType.content("throw new NotImplementedException();");
                else newInstOfSameType.content($"return new {type.RealName()}();");
            }
        }
    }
}
