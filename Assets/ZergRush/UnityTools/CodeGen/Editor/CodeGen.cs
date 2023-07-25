using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ZergRush.Alive;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static List<Type> allTypesInAssemblies = new List<Type>();
        static Dictionary<Type, GenTaskFlags> typeGenRequested = new Dictionary<Type, GenTaskFlags>();
        static Dictionary<string, GenTaskFlags> typeNameRequested = new Dictionary<string, GenTaskFlags>();
        static Queue<GenerationTask> tasks = new Queue<GenerationTask>();

        public static GeneratorContext defaultContext;
        public static Dictionary<string, GeneratorContext> contexts = new Dictionary<string, GeneratorContext>();
        public static Dictionary<Type, GeneratorContext> contextsForTypes = new Dictionary<Type, GeneratorContext>();

        static Dictionary<string, SharpClassBuilder> classes = new Dictionary<string, SharpClassBuilder>();
        static HashSet<string> extensionsSignaturesGenerated = new HashSet<string>();

        static bool hasErrors;
        private static HashSet<string> customContextFolders = new HashSet<string>();

        public static void Error(string err)
        {
            hasErrors = true;
            LogSink.errLog?.Invoke(err);
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

        public static SharpClassBuilder GenClassSink(Type t, GeneratorContext ctx = null)
        {
            var context = ctx ?? GetContext(t);
            if (t.IsControllable() == false && t != typeof(ObjectPool))
            {
                return context.extensionSink;
            }

            if (classes.ContainsKey(t.UniqueName())) return classes[t.UniqueName()];

            var classSink = context.createSharpClass(t.RealName(), t.FileName(), namespaceName: t.Namespace,
                isPartial: true, isStruct: t.IsValueType, isSealed: false);
            classSink.stubMode = stubMode;
            classSink.usingSink("ZergRush.Alive");
            classSink.usingSink("ZergRush");
            classSink.context = context;
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

        static bool ProcessMembers(this Type type, GenTaskFlags currFlag, bool needMembersGen,
            Action<DataInfo> strategy)
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
            return t.IsControllable() && t.HasBaseClassImplementingFlag(flag) &&
                   (flag != GenTaskFlags.DefaultConstructor);
        }

        public static MethodBuilder MakeGenMethod(Type type, GenTaskFlags currTask, string funcName, Type returnType,
            string args,
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

        public static void RegisterTypeContext(Type type, Type requester)
        {
            if (contextsForTypes.ContainsKey(type)) return;
            GenTargetFolder genTargetFolder = type.GetAttribute<GenTargetFolder>(f => f.inheritable);
            if (genTargetFolder != null)
            {
                if (genTargetFolder.folder == null)
                {
                    contextsForTypes[type] = defaultContext;
                }

                if (contexts.TryGetValue(genTargetFolder.folder, out var c) == false)
                {
                    var generatorContext =
                        new GeneratorContext(new GenInfo { sharpGenPath = genTargetFolder.folder }, stubMode);
                    generatorContext.priority = genTargetFolder.priority;
                    contexts[genTargetFolder.folder] = generatorContext;
                    customContextFolders.Add(genTargetFolder.folder);
                    c = generatorContext;
                }

                contextsForTypes[type] = c;
                return;
            }

            if (requester != null)
            {
                contextsForTypes[type] = contextsForTypes[requester];
            }
            else
            {
                contextsForTypes[type] = defaultContext;
            }
        }

        static bool stubMode = false;

        public static void Gen(List<string> includeAssemblies, bool stubs)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblies = includeAssemblies.Select(i => allAssemblies.FirstOrDefault(a => a.GetName().Name == i))
                .Where(a => a != null);
            RawGen(assemblies.ToList(), "Assets/zGenerated", stubs);
        }
    }
}