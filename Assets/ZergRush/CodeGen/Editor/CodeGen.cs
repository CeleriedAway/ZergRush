using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ZergRush.Alive;
using UnityEditor;
using UnityEngine;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static List<Type> allTypesInAssemblies = new List<Type>();
        static Dictionary<Type, GenTaskFlags> typeGenRequested = new Dictionary<Type, GenTaskFlags>();
        static Dictionary<string, GenTaskFlags> typeNameRequested = new Dictionary<string, GenTaskFlags>();
        static Stack<GenerationTask> tasks = new Stack<GenerationTask>();

        const string DefaultGenPath = "Assets/zGenerated/";

        public static GeneratorContext defaultContext => contexts[DefaultGenPath];
        public static Dictionary<string, GeneratorContext> contexts = new Dictionary<string, GeneratorContext>();
        public static Dictionary<Type, GeneratorContext> contextsForTypes = new Dictionary<Type, GeneratorContext>();
        
        static Dictionary<string, SharpClassBuilder> classes = new Dictionary<string, SharpClassBuilder>();
        static HashSet<string> extensionsSignaturesGenerated = new HashSet<string>();

        static bool hasErrors;
        private static HashSet<string> customContextFolders = new HashSet<string>();

        public static void Error(string err)
        {
            Debug.LogError(err);
            hasErrors = true;
        }

        enum Mode
        {
            PartialClass,
            ExtensionMethod
        }

        struct GenerationTask
        {
            public GenerationTask(Type t)
            {
                type = t;
                flags = t.ReadGenFlags();
            }

            public GenerationTask(Type t, GenTaskFlags flags)
            {
                type = t;
                this.flags = flags;
            }

            public Type type;
            public GenTaskFlags flags;
        }
        

        static List<GeneratorContext> tempContexts = new List<GeneratorContext>();
        public static GeneratorContext GetContext(Type t, HashSet<Type> involved = null)
        {
            // if (involved == null) involved = new HashSet<Type>();
            // else if (involved.Contains(t)) return defaultContext;
            // involved.Add(t);
            if (contextsForTypes.TryGetValue(t, out var context)) return context;
            
            if (typeRequestMap.TryGetValue(t, out var requesters))
            {
                tempContexts.Clear();
                foreach (var requester in requesters)
                {
                    if (contextsForTypes.ContainsKey(requester))
                    {
                        tempContexts.Add(contextsForTypes[requester]);
                    }

                    if (tempContexts.Count > 1)
                    {
                        return tempContexts.Best(c => c.priority);
                    }
                    if (tempContexts.Count == 1) return tempContexts[0];
                }
            }
            return defaultContext;
        }

        public static SharpClassBuilder GenClassSink(Type t)
        {
            var context = GetContext(t);
            if (t.IsControllable() == false && t != typeof(ObjectPool))
            {
                return context.extensionSink;
            }
            if (classes.ContainsKey(t.UniqueName())) return classes[t.UniqueName()];
            
            var classSink = context.createSharpClass(t.RealName(), t.FileName(), namespaceName: t.Namespace,
                isPartial: true, isStruct: t.IsValueType, isSealed: false);
            classSink.stubMode = stubMode;
            classSink.usingSink("ZergRush.Alive");
            classes[t.UniqueName()] = classSink;

            // Do not generate constructe generic types... hack
            if (t.IsControllable() && t.IsGenericType && t.IsGenericTypeDecl() == false) classSink.doNotGen = true;
            
            return classSink;
        }

        public static void CheckParameterlessConstructor(this Type t, GenTaskFlags flags)
        {
            if (t.IsValueType || t.GetConstructors().Length == 0) return;
            if ((t.ReadGenFlags() & GenTaskFlags.DefaultConstructor) != 0) return;
            
            var constructor = t.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                Error($"type {t} need a parameterless constructor to support {flags}");
            }
        }

        static string AccessPrefixInGeneratedFunction(this Type t)
        {
            return !t.IsControllable() ? "self" : "";
        }
        
        static bool ProcessMembers(this Type type, GenTaskFlags currFlag, bool needMembersGen, Action<DataInfo> strategy)
        {
            bool hasMembers = false;
            foreach (var member in type.GetMembersForCodeGen(currFlag))
            {
                member.carrierType = type;
                if (needMembersGen && !member.type.IsLoadableConfig()) RequestGen(member.type, type, currFlag);
                member.accessPrefix = type.AccessPrefixInGeneratedFunction();
                strategy(member);
                hasMembers = true;
            }
            return hasMembers;
        }

        public static Type Void => typeof(void);

        static Type TopParentImplementingFlag(this Type type, GenTaskFlags flag)
        {
            Type acceptableClass = null;
            var baseClass = type;
            while (baseClass != null && baseClass != typeof(object))
            {
                if ((baseClass.ReadGenFlags() & flag) != 0)
                {
                    acceptableClass = baseClass;
                }
                baseClass = baseClass.BaseType;
            }
            return acceptableClass;
        }
        static bool HasBaseClassImplementingFlag(this Type type, GenTaskFlags flag)
        {
            var baseClass = type.BaseType;
            while (baseClass != null && baseClass != typeof(object))
            {
                if ((baseClass.ReadGenFlags() & flag) != 0) return true;
                baseClass = baseClass.BaseType;
            }
            return false;
        }
        
        // Base class that skips ignored classes
        static Type ValidBaseClass(this Type type)
        {
            var baseClass = type.BaseType;
            while (baseClass != null && baseClass != typeof(object))
            {
                if (baseClass.IsControllable()) return baseClass;
                baseClass = baseClass.BaseType;
            }
            return baseClass;
        }

        static bool NeedBaseCallForFlag(this Type t, GenTaskFlags flag)
        {
            return t.IsControllable() && t.HasBaseClassImplementingFlag(flag) && (flag != GenTaskFlags.DefaultConstructor);
        }

        public static MethodBuilder MakeGenMethod(Type type, GenTaskFlags currTask, string funcName, Type returnType, string args,
            bool disablebleFirstArg = false)
        {
            bool controllable = type.IsControllable();
            var classSink = GenClassSink(type);

            var mode = controllable ? Mode.PartialClass : Mode.ExtensionMethod;
            
            var genericSuffix = mode == Mode.ExtensionMethod ? type.GenericParametersSuffix() : "";
            var constraints = "";
            if (genericSuffix.Length > 0)
            {
                constraints = type.GenericParametersConstraints();
            }
            
            // TODO-- HACK rewrite
            bool IsCustomImpl = funcName.StartsWith("Base");

            MethodType mType = MethodType.Instance;
            if (!controllable)
            {
                mType = disablebleFirstArg ? MethodType.StaticFunction : MethodType.Extension;
            }
            if (IsCustomImpl)
            {
                mType = MethodType.Instance;
            }
            else if (mode == Mode.PartialClass && type.HasBaseClassImplementingFlag(currTask))
            {
                mType = MethodType.Override;
            }
            else if (type.IsValueType == false && mode == Mode.PartialClass && !type.IsSealed)
            {
                mType = MethodType.Virtual;
            }

            var method = classSink.Method(funcName, type, mType, returnType, args, genericSuffix, constraints);
            method.stubMode = stubMode;
            method.needBaseValCall = type.NeedBaseCallForFlag(currTask);
            
            if (mode == Mode.ExtensionMethod)
            {
                if (extensionsSignaturesGenerated.Contains(method.sig()))
                {
                    method.doNotGen = true;
                }
                extensionsSignaturesGenerated.Add(method.sig());
            }
            
            return method;
        }

        static bool stubMode = false;
        
        // You shall not dare to try to unfuck this.
        public static void Gen(bool stubs)
        {
            if (EditorApplication.isCompiling)
            {
                Debug.LogError("Application is compiling codegen run is not recomended");
                return;
            }

            EditorUtility.DisplayProgressBar("Running codegen", "creating tasks", 0);

            var genDir = new DirectoryInfo(DefaultGenPath);
            if (genDir.Exists == false) {
                genDir.Create();
            }

            // TODO add namespace customization for more performance 
            // bool IsGameType(Type t)
            // {
            //     var typeNamespace = t.Namespace;
            //     var inProject = typeNamespace != null && (
            //         typeNamespace.StartsWith("TWL") || typeNamespace.StartsWith("Game") ||
            //         typeNamespace.StartsWith("Zerg"));
            //     return inProject;
            // }
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("Assembly-CSharp") || a.FullName.StartsWith("ZergRush") || a.FullName.StartsWith("CodeGen")).ToList();
            var typesEnumerable = assemblies.SelectMany(assembly => assembly.GetTypes());

            allTypesInAssemblies.Clear();
            allTypesInAssemblies.AddRange(typesEnumerable.ToList());
            
            typeGenRequested.Clear();
            tasks.Clear();
            genericInstances.Clear();
            polymorphicMap.Clear();
            baseClassMap.Clear();
            extensionsSignaturesGenerated.Clear();
            classes.Clear();
            parents.Clear();
            contexts.Clear();
            customContextFolders.Clear();
            hasErrors = false;
            
            stubMode = stubs;
            contexts[DefaultGenPath] = new GeneratorContext(new GenInfo {sharpGenPath = DefaultGenPath}, stubMode);

            foreach (var type in allTypesInAssemblies)
            {
                if (type.HasAttribute<GenTargetFolder>())
                {
                    var genTargetFolder = type.GetAttribute<GenTargetFolder>();
                    if (contexts.TryGetValue(genTargetFolder.folder, out var c) == false)
                    {
                        var generatorContext = new GeneratorContext(new GenInfo{sharpGenPath = genTargetFolder.folder}, stubMode);
                        generatorContext.priority = genTargetFolder.priority;
                        contexts[genTargetFolder.folder] = generatorContext;
                        customContextFolders.Add(genTargetFolder.folder);
                        c = generatorContext;
                    }
                    contextsForTypes[type] = c;
                }
                
                foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (methodInfo.HasAttribute<CodeGenExtension>())
                    {
                        methodInfo.Invoke(null, null);
                    }
                }

                GenTaskFlags readGenFlags = GenTaskFlags.None;;
                if ((readGenFlags = type.ReadGenFlags()) != GenTaskFlags.None)
                {
                    RequestGen(type, null, readGenFlags, true);
                }
            }

            
            while (tasks.Count > 0)
            {
                var task = tasks.Pop();
                var type = task.type;
                
                if (type.HasAttribute<DoNotGen>()) continue;
                
                var classSink = GenClassSink(task.type);

                classSink.indent++;

                Action<GenTaskFlags, Action<string>> checkFlag = (flag, gen) =>
                {
                    if ((task.flags & flag) != 0)
                    {
                        bool isCustom = false;
                        bool needGenBase = false;
                        var genTaskCustomImpl = type.GetCustomImplAttr();
                        if (genTaskCustomImpl != null)
                        {
                            if ((genTaskCustomImpl.flags & flag) != 0)
                            {
                                isCustom = true;
                                needGenBase = genTaskCustomImpl.genBaseMethods;
                            }
                        }

                        if (isCustom && needGenBase == false) 
                            return;
                        string funcPrefix = isCustom ? "Base" : "";
                        gen(funcPrefix);
                    }
                };

                checkFlag(GenTaskFlags.UpdateFrom, funcPrefix => GenUpdateFrom(type, false, funcPrefix));
                checkFlag(GenTaskFlags.PooledUpdateFrom, funcPrefix => GenUpdateFrom(type, true, funcPrefix));
                checkFlag(GenTaskFlags.Deserialize, funcPrefix => GenerateDeserialize(type, false, funcPrefix));
                checkFlag(GenTaskFlags.PooledDeserialize, funcPrefix => GenerateDeserialize(type, true, funcPrefix));
                checkFlag(GenTaskFlags.Serialize, funcPrefix => GenerateSerialize(type, funcPrefix));
                checkFlag(GenTaskFlags.Hash, funcPrefix => GenHashing(type, funcPrefix));
                checkFlag(GenTaskFlags.UIDGen, funcPrefix => GenUIDFunc(type, funcPrefix));
                checkFlag(GenTaskFlags.CollectConfigs, funcPrefix => GenCollectConfigs(type, funcPrefix));
                checkFlag(GenTaskFlags.LifeSupport, funcPrefix => GenerateLivable(type, funcPrefix));
                checkFlag(GenTaskFlags.OwnershipHierarchy, funcPrefix => GenerateHierarchyAndId(type, funcPrefix));
                checkFlag(GenTaskFlags.OwnershipHierarchy, funcPrefix => GenerateConstructionFromRoot(type));
                checkFlag(GenTaskFlags.DefaultConstructor, funcPrefix => GenerateConstructor(type));
                checkFlag(GenTaskFlags.CompareChech, funcPrefix => GenerateComparisonFunc(type, funcPrefix));
                checkFlag(GenTaskFlags.JsonSerialization, funcPrefix => GenerateJsonSerialization(type, funcPrefix));
                checkFlag(GenTaskFlags.Pooled, funcPrefix => GeneratePoolSupportMethods(type));
                //checkFlag(GenTaskFlags.PrintHash, funcPrefix => GeneratePrintHash(type, funcPrefix));

                classSink.indent--;
            }

            GenerateFieldWrappers();
            GeneratePolimorphismSupport();
            GeneratePolymorphicRootSupport();
            // Do not change anythign if there is any errors
            if (hasErrors)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            EditorUtility.DisplayProgressBar("Running codegen", "writing cs files", 0.5f);

            customContextFolders.Add(DefaultGenPath);
            customContextFolders.ForEach(genFolder =>
            {
                if (Directory.Exists(genFolder) == false)
                {
                    Directory.CreateDirectory(genFolder);
                    return;
                }
                foreach (FileInfo file in new DirectoryInfo(genFolder).GetFiles())
                {
                    // Skip metafiles for clean commit messages.
                    if (file.Name.EndsWith("meta") || file.Name.EndsWith("txt")) continue;
                    file.Delete();
                }
            });

            foreach (var typeEnumTable in finalTypeEnum)
            {
                EnumTable.Save(typeEnumTable.Key.TypeTableFileName(), new EnumTable{records = typeEnumTable.Value});
            }
            
            foreach (var context in contexts.Values)
            {
                context.Commit();
            }
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
        }
    }
}

            // Lenses... Curently not needed
//            foreach (var type in typesList)
//            {
//                foreach (var member in type.GetMembersForCodeGen(GenTaskFlags.None))
//                {
//                    if (member.isReference)
//                    {
//                        var sink = GenClassSink(type);
//                        sink.content(
//                            $"\tstatic Action<object, object> {SetterName(member.name)} = (self, obj) => (({type.RealName(true)})other).{member.name} = ({member.type.RealName(true)})(obj);");
//                    }
//                }
//            }




//                        if (type.IsToolBased())
//                        {
//                            // Iterate static data
//                            sink.content($"if (this.{ToolPrototypeIdGetter}() != {otherName}.{ToolPrototypeIdGetter}())");
//                            sink.content("{");
//                            sink.indentLevel++;
//                            sink.content($"{ToolResetFunc}();");
//                            processMembers((path, mt, memberInfo, valTransformer) =>
//                            {
//                                if (mt.IsLoadableConfig() == false) return;
//                                GenUpdateValueFrom(sink, mt, path,
//                                    valTransformer($"{otherName}.{memberInfo.name}"), memberInfo.canBeNull, memberInfo.isStatic);
//                            }, GenTaskFlags.UpdateFrom);
//                            sink.content($"{ToolInitFuncName}();");
//                            sink.indentLevel--;
//                            sink.content("}");
//                            
//                            processMembers((path, mt, memberInfo, valTransformer) =>
//                            {
//                                if (mt.IsLoadableConfig()) return;
//                                GenUpdateValueFrom(sink, mt, path,
//                                    valTransformer($"{otherName}.{memberInfo.name}"), memberInfo.canBeNull, memberInfo.isStatic, memberInfo.isReference);
//                            }, GenTaskFlags.UpdateFrom);
//                        }
