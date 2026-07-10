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

        public static bool NeedsClassicPolymorphConstruction(this Type t)
        {
            return (t.ReadGenFlags() & GenTaskFlags.PolymorphicConstruction) != 0;
        }

        public static bool CanBeAncestor(this Type t)
        {
            if (t.IsSealed) return false;
            return t.ChildTypes.Count > 0;
        }


        static IEnumerable<Type> PolymorphicGenerationCandidates()
        {
            return allTypesInAssemblies
                .Concat(typeGenRequested.Keys)
                .Concat(genericInstances.Values.SelectMany(types => types))
                .Where(t => t != null)
                .Distinct();
        }

        static IEnumerable<Type> PolymorphicConstructionRoots()
        {
            return PolymorphicGenerationCandidates()
                .Select(t => t.PolymorphicConstructionRoot())
                .Where(t => t != null)
                .Distinct()
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name);
        }

        static IEnumerable<Type> PolymorphicConstructionTypes(Type root)
        {
            return PolymorphicGenerationCandidates()
                .Where(t => t.PolymorphicConstructionRoot() == root)
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name);
        }

        static Type PolymorphicConstructionRoot(this Type t)
        {
            Type root = null;
            for (var current = t; current != null; current = current.BaseType)
            {
                if ((current.Flags & GenTaskFlags.PolymorphicConstruction) != 0)
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

        static Type PolymorphicConstructionRootOrSelf(this Type t)
        {
            return t.PolymorphicConstructionRoot() ?? t;
        }

        static string NewPolymorphicFromClassIdExpression(this Type type, bool pooled)
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
                foreach (var type in genericInstances[genericDef])
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

        static void GeneratePolimorphismSupport()
        {
            foreach (var baseClass in PolymorphicConstructionRoots())
            {
                var sink = GenClassSink(baseClass);

                var typesToGenPolymorphMethods = PolymorphicConstructionTypes(baseClass).ToList();
                var typesThatCanBeConstructed = typesToGenPolymorphMethods.Where(t => t.IsValidType()).ToList();

                var fileName = baseClass.TypeTableFileName();

                var typeTable = EnumTable.Load(fileName);
                var validTypes = typesThatCanBeConstructed.Where(t => t.IsAbstract == false && t.IsValidType())
                    .ToList();
                typeTable.UpdateWithNewTypes(validTypes.Select(t => t.UniqueName(false)));

                var finalTypeIndexedList = new List<Type>();
                foreach (var type in validTypes)
                {
                    var index = typeTable.records[type.UniqueName(false)];
                    finalTypeIndexedList.EnsureSizeWithNulls(index + 1);
                    finalTypeIndexedList[index] = type;
                }

                EnumTable.PrintEnum(sink, TypeEnumName, typesThatCanBeConstructed.Where(t => t.IsValidType())
                        .Where(t => t.IsAbstract == false).Select(t => t.UniqueName(false)),
                    type => typeTable.records[type]);

                GenClassIdFuncs(baseClass, typesToGenPolymorphMethods, sink);

                if (baseClass.NeedsClassicPolymorphConstruction()
                    || ((baseClass.ReadGenFlags() & (GenTaskFlags.UpdateFrom | GenTaskFlags.Serialization)) != 0))
                {
                    GenPolymorphicRootSetup(baseClass, sink, finalTypeIndexedList);
                    GenPolymorphMaps(baseClass, typesThatCanBeConstructed, typesToGenPolymorphMethods, sink);
                }

                var rootEnumName = baseClass.PolymorphicRootTypeEnumName();
                var module = sink.module;
                var c = new GeneratorContext(new GenInfo {sharpGenPath = module.path});
                contexts.Add(rootEnumName, c);
                module = c.createSharpCustomModule($"{rootEnumName}", "enum");
                module.content("");
                if (!string.IsNullOrEmpty(sink.namespaceName))
                {
                    module.content($"namespace {sink.namespaceName} {{");
                    module.indent++;
                }

                EnumTable.PrintEnum(module, rootEnumName, validTypes.Select(t => t.UniqueName(false)),
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
                creatorFunc.content($"return {baseClass.NewPolymorphicFromClassIdExpression(false)};");
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

        static void GenClassIdFuncs(Type baseClass, List<Type> typesToGenPolymorphMethods, SharpClassBuilder sink)
        {
            foreach (var type in typesToGenPolymorphMethods)
            {
                if (type.ReadGenCustomFlags() == type.ReadGenFlags())
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

        static void GenPolymorphMaps(Type baseClass, List<Type> typesThatCanBeConstructed,
            List<Type> typesToGenPolymorphMethods, SharpClassBuilder sink)
        {
            // Class id overloaded functions
            foreach (var type in typesToGenPolymorphMethods)
            {
                if (type.ReadGenCustomFlags() == type.ReadGenFlags())
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
