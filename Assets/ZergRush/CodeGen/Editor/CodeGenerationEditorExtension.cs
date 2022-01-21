using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ZergRush.CodeGen
{
    public static class CodeGenerationEditorExtension
    {
        private static readonly HashSet<string> includeAssemblies = new HashSet<string>
        {
            "ZergRush",
            "ZergRush.CodeGen",
            "ZergRush.Reactive",
            "ZergRush.Utils",
            "CodeGen",
            "ClientServerShared",
            "SharedCode",
            "Assembly-CSharp",
            "Assembly-CSharp-Editor",
            "AGameServerShared"
        };
        
        [MenuItem("Code Gen/Run CodeGen #&c")]
        public static void GenCode()
        {
            GenerateInner(includeAssemblies);
        }
        
        [MenuItem("Code Gen/Generate Stubs #&s")]
        public static void GenStubs()
        {
            GenerateInner(includeAssemblies, onlyStubs: true);
        }

        public static void GenerateInner(HashSet<string> assemblies, bool onlyStubs = false)
        {
            Debug.Log("GenCode called");
            CodeGen.Gen(assemblies, onlyStubs);
        }
    }
}