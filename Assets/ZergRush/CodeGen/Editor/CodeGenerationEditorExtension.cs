using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ZergRush;
using ZergRush.ReactiveCore;

namespace ZergRush.CodeGen
{
    public static class CodeGenerationEditorExtension
    {
        [MenuItem("Code Gen/Run CodeGen #&c")]
        public static void GenCode()
        {
            GenerateInner();
        }
        
        [MenuItem("Code Gen/Generate Stubs #&s")]
        public static void GenStubs()
        {
            GenerateInner(onlyStubs: true);
        }

        static void GenerateInner(bool onlyStubs = false)
        {
            Debug.Log("GenCode called");
            ZergRush.CodeGen.CodeGen.Gen(onlyStubs);
        }
    }
}