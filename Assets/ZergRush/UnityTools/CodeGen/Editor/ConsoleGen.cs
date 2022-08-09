using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ZergRush.CodeGen
{
    public static partial class CodeGen
    {
        public static void RawGen(List<Assembly> assemblies, bool stubs)
        {
            var genDir = new DirectoryInfo(DefaultGenPath);
            if (genDir.Exists == false)
            {
                genDir.Create();
            }

            Console.WriteLine($"Order: {assemblies.Select(a => a.GetName().Name).PrintCollection()}");
            var typesEnumerable = assemblies.SelectMany(assembly => assembly.GetTypes());

            List<string> pathPartPriority = new List<string>
            {
                "ZergRush",
                "AGameServerShared",
                "SharedCode"
            };
            pathPartPriority.Reverse();
            allTypesInAssemblies.Clear();
            allTypesInAssemblies.AddRange(typesEnumerable.ToList());
            var priorityList = allTypesInAssemblies.Select(t => {
                var tf = t.GetAttribute<GenTargetFolder>();
                if (tf != null)
                {
                    return (t, pathPartPriority.IndexOf(p => tf.folder.Contains(p)));
                }
                return (t, -2);
            }).ToList();
            priorityList.Sort((tp1, tp2) =>
            {
                if (tp1.Item2 != tp2.Item2) return tp2.Item2.CompareTo(tp1.Item2);
                var t1 = tp1.t;
                var t2 = tp2.t;
                return (t1.Namespace, t1.Name).CompareTo((t2.Namespace, t2.Name));
            });
            allTypesInAssemblies.Clear();
            allTypesInAssemblies.AddRange(priorityList.Select(p => p.t));

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

            foreach (var typeAndPriority in priorityList)
            {
                var typeInAssembly = typeAndPriority.t;
                
                RegisterTypeContext(typeInAssembly, null);
                foreach (var methodInfo in typeInAssembly.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                           BindingFlags.Static))
                {
                    if (methodInfo.HasAttribute<CodeGenExtension>())
                    {
                        methodInfo.Invoke(null, null);
                    }
                }

                GenTaskFlags readGenFlags = GenTaskFlags.None;
                
                if ((readGenFlags = typeInAssembly.ReadGenFlags()) != GenTaskFlags.None)
                {
                    RequestGen(typeInAssembly, null, readGenFlags, true);
                }

                while (tasks.Count > 0)
                {
                    var task = tasks.Dequeue();
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
                    checkFlag(GenTaskFlags.PooledDeserialize,
                        funcPrefix => GenerateDeserialize(type, true, funcPrefix));
                    checkFlag(GenTaskFlags.Serialize, funcPrefix => GenerateSerialize(type, funcPrefix));
                    checkFlag(GenTaskFlags.Hash, funcPrefix => GenHashing(type, funcPrefix));
                    checkFlag(GenTaskFlags.UIDGen, funcPrefix => GenUIDFunc(type, funcPrefix));
                    checkFlag(GenTaskFlags.CollectConfigs, funcPrefix => GenCollectConfigs(type, funcPrefix));
                    checkFlag(GenTaskFlags.LifeSupport, funcPrefix => GenerateLivable(type, funcPrefix));
                    checkFlag(GenTaskFlags.OwnershipHierarchy, funcPrefix => GenerateHierarchyAndId(type, funcPrefix));
                    checkFlag(GenTaskFlags.OwnershipHierarchy, funcPrefix => GenerateConstructionFromRoot(type));
                    checkFlag(GenTaskFlags.DefaultConstructor, funcPrefix => GenerateConstructor(type, funcPrefix));
                    checkFlag(GenTaskFlags.CompareChech, funcPrefix => GenerateComparisonFunc(type, funcPrefix));
                    checkFlag(GenTaskFlags.JsonSerialization,
                        funcPrefix => GenerateJsonSerialization(type, funcPrefix));
                    checkFlag(GenTaskFlags.Pooled, funcPrefix => GeneratePoolSupportMethods(type));
                    //checkFlag(GenTaskFlags.PrintHash, funcPrefix => GeneratePrintHash(type, funcPrefix));

                    classSink.indent--;
                }
            }


            GenerateFieldWrappers();
            GeneratePolimorphismSupport();
            GeneratePolymorphicRootSupport();
            // Do not change anythign if there is any errors
            if (hasErrors)
            {
                LogSink.errLog("error occured");
                return;
            }

            //EditorUtility.DisplayProgressBar("Running codegen", "writing cs files", 0.5f);

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
                EnumTable.SaveEnumCache(typeEnumTable.Key.TypeTableFileName(),
                    new EnumTable {records = typeEnumTable.Value});
            }

            foreach (var context in contexts.Values)
            {
                context.Commit();
            }

            LogSink.log("codegen complete");
        }
    }
}