using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

namespace ZergRush.CodeGen
{
// Contains required paths for file creation
    public class GenInfo
    {
        public string sharpGenPath = "";
    }

    public abstract class CodeBuilder
    {
        public string name;
        public string path;
        public string suffix;
        public abstract void Commit();
    }

    public class GeneratorContext
    {
        public int priority;
        private string pathToSharp => context.sharpGenPath;
        List<CodeBuilder> builders = new List<CodeBuilder>();
        HashSet<string> builderFileNames = new HashSet<string>();
        private GenInfo context;
        public SharpClassBuilder extensionSink;
        bool stubModel;
        
        public GeneratorContext(GenInfo context, bool stubMode)
        {
            this.context = context;
            this.stubModel = stubMode;
            
            extensionSink = createSharpClass("SerializationExtensions", isStatic: true, isPartial:true);
            extensionSink.usingSink("System.IO");
            extensionSink.usingSink("ZergRush.Alive");
        }

        public SharpClassBuilder createSharpClass(string name, string fileName = "", string namespaceName = "", 
            bool isStruct = false, bool isSealed = false, bool isPartial = false, bool isStatic = false)
        {
            if (fileName.Length == 0) fileName = name;
            var module = createSharpCustomModule(fileName);
            var builder = module.Class(name, namespaceName, isStruct, isSealed, isPartial, isStatic);
            return builder;
        }

        public SharpCustomModule createSharpCustomModule(string name)
        {
            if (builderFileNames.Contains(name))
            {
                Debug.LogError($"builder: {name} is already created");
            }
            var builder = new SharpCustomModule()
            {
                name = name,
                path = pathToSharp,
                suffix = "gen",
                stubMode = stubModel
            };
            builderFileNames.Add(name);
            builders.Add(builder);
            return builder;
        }

        public void Commit()
        {
            foreach (var builder in builders)
            {
                builder.Commit();
            }
        }

        public static string PrepareToWrite(string code)
        {
            return code.Replace("\t", "    ");
        }
    }
}

#endif
