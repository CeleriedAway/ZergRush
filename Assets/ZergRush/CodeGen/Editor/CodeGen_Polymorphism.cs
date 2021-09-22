using System;
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
        public static readonly string PolymorphReturnToPool = "ReturnToPool";
        public static readonly string PolymorphReturnChildrenToPool = "ReturnChildrenToPool";
        public static readonly string GenericPoolGetter = "GetPoolForGenericType";
        const string TypeEnumName = "Types";

        static Dictionary<Type, HashSet<Type>> polymorphicMap = new Dictionary<Type, HashSet<Type>>();
        static Dictionary<Type, Type> baseClassMap = new Dictionary<Type, Type>();
        static HashSet<Type> parents = new HashSet<Type>();
        static HashSet<Type> pooledPolymorphicConstructors = new HashSet<Type>();
        static HashSet<Type> normalPolymorphicConstructors = new HashSet<Type>();
        static Dictionary<Type, HashSet<Type>> polymorphicRootNodes = new Dictionary<Type, HashSet<Type>>();

        static Dictionary<Type, Dictionary<string, int>>
            finalTypeEnum = new Dictionary<Type, Dictionary<string, int>>();

        static Dictionary<Type, HashSet<Type>> genericInstances = new Dictionary<Type, HashSet<Type>>();

        public static string PolymorphInstanceFuncNamePooled(bool pooled)
        {
            return PolymorphInstanceFuncName + (pooled ? "Pooled" : "");
        }

        public static string PolymorphicRootTypeEnumName(this Type t)
        {
            return t.UniqueName(false) + "Type";
        }

        public static bool NeedsPolymorphRegistration(this Type t)
        {
            return (t.ReadGenFlags() &
                    (GenTaskFlags.PolymorphicConstruction | GenTaskFlags.PooledPolymorphicConstruction)) != 0;
        }

        public static bool NeedsClassicPolymorphConstruction(this Type t)
        {
            return (t.ReadGenFlags() & GenTaskFlags.PolymorphicConstruction) != 0;
        }

        public static bool NeedsPooledPolymorphConstruction(this Type t)
        {
            return (t.ReadGenFlags() & GenTaskFlags.PooledPolymorphicConstruction) != 0;
        }

        static void RegisterPolymorph(Type t)
        {
            if (t.NeedsPolymorphRegistration() == false) return;

            if ((t.ReadGenFlags() & GenTaskFlags.PolymorphicConstruction) != 0)
            {
                normalPolymorphicConstructors.Add(t);
            }

            if ((t.ReadGenFlags() & GenTaskFlags.PooledPolymorphicConstruction) != 0)
            {
                pooledPolymorphicConstructors.Add(t);
            }

            Type lastValidParent = null;
            var parent = t.BaseType;
            while (parent != null)
            {
                if (parent.NeedsPolymorphRegistration())
                {
                    if (parent.HasAttribute<GenPolymorphicNode>())
                    {
                        polymorphicRootNodes.TryGetOrNew(parent).Add(t);
                    }

                    parents.Add(parent);
                    lastValidParent = parent;
                }

                parent = parent.BaseType;
            }

            if (lastValidParent != null)
            {
                polymorphicMap.TryGetOrNew(lastValidParent).Add(t);
                baseClassMap[t] = lastValidParent;
            }
        }

        public static bool NeedClassIdCache(this Type t)
        {
            return baseClassMap.ContainsKey(t) || polymorphicMap.ContainsKey(t);
        }

        public static bool CanBeAncestor(this Type t)
        {
            if (t.IsSealed) return false;
            if (t.IsGenericParameter)
            {
                foreach (var genericParameterConstraint in t.GetGenericParameterConstraints())
                {
                    if (genericParameterConstraint.IsClass)
                        return genericParameterConstraint.CanBeAncestor();
                }
            }

            if (t.IsGenericType && t.BaseType != null
            ) // IsGenericType == true also when it just implements generic interface.
            {
                return t.BaseType.CanBeAncestor();
            }

            return t.IsAbstract ||
                   (baseClassMap.ContainsKey(t) || polymorphicMap.ContainsKey(t)) && parents.Contains(t);
        }

        public static Type TypeToUpdateFrom(this Type t)
        {
            return baseClassMap.GetOrDefault(t, t);
        }

        static bool RequirePolymorphConstruct(this Type t)
        {
            return polymorphicMap.ContainsKey(t);
        }


        static void GeneratePolymorphicRootSupport()
        {
            foreach (var polymorphicRootNode in polymorphicRootNodes)
            {
                var node = polymorphicRootNode.Key;
                var types = polymorphicRootNode.Value
                    .Where(t => t.IsValidType())
                    .Where(t => t.IsAbstract == false);
                var nodeClass = GenClassSink(node);
                var enumName = node.PolymorphicRootTypeEnumName();

                var module = nodeClass.module;
                module.content("");
                if (!string.IsNullOrEmpty(nodeClass.namespaceName))
                {
                    module.content($"namespace {nodeClass.namespaceName} {{");
                    module.indent++;
                }

                EnumTable.PrintEnum(module, enumName, types.Select(t => t.UniqueName(false)),
                    type => finalTypeEnum[baseClassMap[node]][type]);
                if (!string.IsNullOrEmpty(nodeClass.namespaceName))
                {
                    module.indent--;
                    module.content($"}}");
                }

                module.content("");
            }

            GeneratePolymorphicCreatorFuncs();
        }

        static void GeneratePolymorphicCreatorFuncs()
        {
            foreach (var polymorphicRootNode in polymorphicRootNodes)
            {
                var node = polymorphicRootNode.Key;
                var nodeClass = GenClassSink(node);
                var enumName = node.PolymorphicRootTypeEnumName();

                Action<bool> gen = pooled =>
                {
                    var poolCreatorFunc = nodeClass.Method(PolymorphInstanceFuncNamePooled(pooled), node,
                        MethodType.StaticFunction, node,
                        $"{enumName} classId{node.OptPoolSecondArgDecl(pooled)}", "", "");
                    poolCreatorFunc.content($"return {node.NewPolymorphicFromClassIdExpression(pooled)};");
                };
                if (node.NeedsClassicPolymorphConstruction()) gen(false);
                if (node.NeedsPooledPolymorphConstruction()) gen(true);
                nodeClass.content(
                    $"public {node.PolymorphicRootTypeEnumName()} type => ({node.PolymorphicRootTypeEnumName()}) GetClassId();");
            }
        }

        static string NewPolymorphicFromClassIdExpression(this Type type, bool pooled)
        {
            return
                $"({type.RealName(true)}){type.RealName(true)}.{PolymorphInstanceFuncNamePooled(pooled)}(({PolymorphClassIdTypeName}) classId{type.OptPoolSecondArg(pooled)})";
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
            return DefaultGenPath + "types_cache_" + t.Name + ".txt";
        }

        static void GeneratePoolSupportMethods(Type type)
        {
            if (type.IsValidType() == false) return;

            if (type.IsAbstract == false && type.IsValidType())
            {
                var pool = typeof(ObjectPool);
                var poolClass = GenClassSink(pool);
                string poolType = $"Pool<{type.RealName(true)}>";
                string prototypeName = $"prototype{type.UniqueName()}";
                poolClass.content($"public {poolType} {type.PersonalPoolName()} = new {poolType}();");
                poolClass.content($"public {type.RealName(true)} {prototypeName};");
                var getFromPool = poolClass.Method(type.GetFromPoolFunc(), typeof(ObjectPool), MethodType.Instance,
                    type,
                    "",
                    "", "");
                getFromPool.content($"{type.RealName(true)} inst = null;");
                getFromPool.content(
                    $"if ({type.PersonalPoolName()}.Count > 0) {{ inst = {type.PersonalPoolName()}.Pop();");
                getFromPool.indent++;
                getFromPool.content($"if ({prototypeName} == null) {prototypeName} = new {type.RealName(true)}();");
                getFromPool.content($"inst.UpdateFrom({prototypeName}, this);");
                getFromPool.indent--;
                getFromPool.content($"}}");
                getFromPool.content($"else inst = new {type.RealName(true)}();");
                getFromPool.content($"return inst;");
            }

            var returnToPoolMethod = MakeGenMethod(type, GenTaskFlags.Pooled, PolymorphReturnToPool, Void,
                type.OptPoolArgDecl(true));
            returnToPoolMethod.doNotCallBaseMethod = true;

            if (type.IsAbstract)
            {
                returnToPoolMethod.content("throw new NotImplementedException();");
            }
            else
            {
                if (type.IsGenericTypeDecl())
                    returnToPoolMethod.content($"{GenericPoolGetter}(pool).PushGeneric(this);");
                else returnToPoolMethod.content($"pool.{type.PersonalPoolName()}.Push(this);");
                returnToPoolMethod.content($"{PolymorphReturnChildrenToPool}(pool);");
                if (type.IsLivableGen())
                {
                    returnToPoolMethod.content("root = null;");
                    returnToPoolMethod.content("carrier = null;");
                }
            }

            var returnToPoolChildrenMethod = MakeGenMethod(type, GenTaskFlags.Pooled,
                PolymorphReturnChildrenToPool, Void,
                type.OptPoolArgDecl(true));

            // Command all containers to push all its content back to pool.
            ProcessMembers(type, GenTaskFlags.Pooled, false, info =>
            {
                if (info.type.IsLivableSlot() || info.isValueWrapper == ValueVrapperType.LivableSlot ||
                    info.type.IsLivableList())
                {
                    returnToPoolChildrenMethod.content($"{info.baseAccess}.OnReturnToPool(pool);");
                }
            });
        }

        static void GeneratePolimorphismSupport()
        {
            foreach (var polymorphTask in polymorphicMap)
            {
                var baseClass = polymorphTask.Key;
                var children = polymorphTask.Value;
                var sink = GenClassSink(baseClass);

                var typesToGenPolymorphMethods = new List<Type> {baseClass}.Concat(children).ToList();
                var typesThatCanBeConstructed = typesToGenPolymorphMethods.Where(t => t.IsValidType()).ToList();

                var fileName = baseClass.TypeTableFileName();

                var typeTable = EnumTable.Load(fileName);
                var validTypes = typesThatCanBeConstructed.Where(t => t.IsAbstract == false && t.IsValidType())
                    .ToList();
                typeTable.UpdateWithNewTypes(validTypes.Select(t => t.UniqueName(false)));
                finalTypeEnum[baseClass] = typeTable.records;

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

                if (baseClass.NeedsPooledPolymorphConstruction())
                {
                    GenPolymorphicRootSetup(baseClass, sink, finalTypeIndexedList, true);
                    GenPolymorphMaps(baseClass, typesThatCanBeConstructed, typesToGenPolymorphMethods, sink, true);
                }

                if (baseClass.NeedsClassicPolymorphConstruction()
                    || (!baseClass.HasPool() && (baseClass.ReadGenFlags()
                                                 & (GenTaskFlags.UpdateFrom | GenTaskFlags.Serialization)) != 0))
                {
                    GenPolymorphicRootSetup(baseClass, sink, finalTypeIndexedList, false);
                    GenPolymorphMaps(baseClass, typesThatCanBeConstructed, typesToGenPolymorphMethods, sink, false);
                }
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
            List<Type> typeIndexer, bool pooled)
        {
            string poolTypeArgIfAny = pooled ? $"{baseClass.PoolTypeName()} ," : "";
            if (pooled)
            {
                sink.usingSink("ZergRush.Alive");
            }

            // Array with constructors
            var constructorsArrayName = $"polymorph{(pooled ? "Pulled" : "")}Constructors";
            sink.content(
                $"static Func<{poolTypeArgIfAny}{baseClass.RealName()}> [] {constructorsArrayName} =" +
                $" new Func<{poolTypeArgIfAny}{baseClass.RealName()}> [] {{");
            sink.indent++;
            if (stubMode == false)
            {
                for (var i = 0; i < typeIndexer.Count; i++)
                {
                    var type = typeIndexer[i];
                    sink.content(
                        $"{(pooled ? "pool" : "()")} => {(type != null ? NewInstExpr(type, pooled) : "null")}, // {i}");
                }
            }

            sink.indent--;
            sink.content($"}};");

            // Create function
            sink.content(
                $"public static {baseClass.RealName()} {PolymorphInstanceFuncNamePooled(pooled)}(" +
                $"{PolymorphClassIdType} typeId{baseClass.OptPoolSecondArgDecl(pooled)}) {{");
            sink.content($"\treturn {constructorsArrayName}[typeId]({(pooled ? "pool" : "")});");
            sink.content($"}}");
        }

        static void GenPolymorphMaps(Type baseClass, List<Type> typesThatCanBeConstructed,
            List<Type> typesToGenPolymorphMethods, SharpClassBuilder sink, bool pooledMap)
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

                var pooled = type.HasPool() && pooledMap;

                var newInstOfSameType = tSink.Method(PolymorphNewInstOfSameType, type, mType, baseClass,
                    pooledMap ? $"{PoolTypeName(null)} pool" : "");

                newInstOfSameType.doNotCallBaseMethod = true;

                if (type.Name == "DataNode")
                {
                    // a hack required to make good application template
                    newInstOfSameType.doNotGen = true;
                }
                else if (type.IsAbstract) newInstOfSameType.content("throw new NotImplementedException();");
                else if (type.IsGenericTypeDecl() && pooled)
                {
                    var genericPoolGetter = tSink.Method(GenericPoolGetter, type, MethodType.Instance,
                        typeof(IGenericPool), type.OptPoolArgDecl(type.HasPool()));
                    PrintGenericSwitch(type, genericPoolGetter,
                        (t, s) => s.content($"return pool.{t.PersonalPoolName()};"));
                    genericPoolGetter.content("return null;");
                    newInstOfSameType.content(
                        $"return ({type.RealName()}){GenericPoolGetter}(pool).PopGeneric();");
                }
                else if (pooled) newInstOfSameType.content($"return pool.{type.GetFromPoolFunc()}();");
                else newInstOfSameType.content($"return new {type.RealName()}();");

                if (pooled)
                {
                }
            }
        }
    }
}