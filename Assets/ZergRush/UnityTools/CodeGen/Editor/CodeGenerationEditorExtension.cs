#if UNITY_EDITOR

using System;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ZergRush.CodeGen
{
    public static class CodeGenerationEditorExtension
    {
        private static readonly List<string> includeAssemblies = new List<string>
        {
            "ZergRush.Core",
            "ZergRush.Unity",
            "ClientServerShared",
            "SharedCode",
            "AGameServerShared",
            "Assembly-CSharp",
            "Assembly-CSharp-Editor",
        };

        public static readonly bool IsWindows = Application.platform == RuntimePlatform.WindowsEditor;

        // No need to look for it on Mac - assuming we got it through `brew install dotnet-sdk` - @ micktu
        public static readonly string DotnetExecutablePath = IsWindows ? "dotnet" : "/usr/local/bin/dotnet";

        static bool hasErrors;
        
        [InitializeOnLoadMethod]
        static void CodeGenerationEditorExtensionInit()
        {
            CompilationPipeline.assemblyCompilationFinished += (s, messages) =>
            {
                hasErrors = messages.Any(m => m.type == CompilerMessageType.Error);
                Debug.Log($"~~~~~~~~~~~~ {hasErrors}");
            };
        }

        [MenuItem("Code Gen/Run CodeGen #&c")]
        public static void GenCode()
        {
            if (hasErrors) GenCodeConsole();
            else GenCodeClassic();
            AssetDatabase.Refresh();
        }

        [MenuItem("Code Gen/Force CodeGen Classic")]
        public static void GenCodeClassic()
        {
            EditorUtility.DisplayProgressBar(CCDTitle, "Running CodeGen...", 0.0f);
            try
            {
                GenerateInner(includeAssemblies);
                EditorUtility.DisplayProgressBar(CCDTitle, "Finishing...", 1f);
            }
            catch (Exception e)
            {
                Debug.LogError("Codegen failed with exception: " + e.ToError());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        // [MenuItem("Code Gen/Run CodeGen Stub #&s")]
        // public static void GenCodeStubs()
        // {
        //     GenerateInner(includeAssemblies, true);
        //     AssetDatabase.Refresh();
        // }

        static string CCDTitle = "Console Codegen";

        [MenuItem("Code Gen/Force CodeGen Console")]
        public static void GenCodeConsole()
        {
            try
            {
                EditorUtility.DisplayProgressBar(CCDTitle, "Compiling CodeGen solution...", 0f);
                var path = ExePath();
                RunCompilation();
                EditorUtility.DisplayProgressBar(CCDTitle, "Running CodeGen...", 0.5f);
                RunProcessAndReadLogs(path, $" {string.Join(' ', includeAssemblies)}", Path.GetDirectoryName(path));
                EditorUtility.DisplayProgressBar(CCDTitle, "Finishing...", 1f);
                if (File.Exists(path) == false)
                {
                    Debug.LogError("compilation did not produce exe");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Codegen console failed with exception: " + e.ToError());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        static void RunProcessAndReadLogs(string fileName, string args, [JetBrains.Annotations.CanBeNull] string dir)
        {
            Process p = new Process();
            var si = p.StartInfo;
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.FileName = fileName;
            si.Arguments = args;
            if (dir != null)
                si.WorkingDirectory = Path.GetFullPath(dir);

            p.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) Debug.LogError(e.Data);
            };
            p.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) Debug.Log(e.Data);
            };

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            p.WaitForExit();
        }

        public static void GenerateInner(List<string> assemblies, bool onlyStubs = false)
        {
            Debug.Log("GenCode called");
            CodeGen.Gen(assemblies, onlyStubs);
        }

        static string SearchSolution(string path)
        {
            foreach (var enumerateFile in Directory.GetFiles(path, "ConsoleGen.sln", SearchOption.AllDirectories))
            {
                return enumerateFile;
            }

            throw new ZergRushException($"cant find zergrush solution file at path {path}");
        }

        private static string GetSolutionFilePath()
        {
            string solutionFolder;
            var packagePath = Path.Combine("Packages", "ZergRush", "Assets", "ZergRush", "UnityTools", "CodeGen");
            if (Directory.Exists(packagePath))
            {
                solutionFolder = SearchSolution(packagePath);
            }
            else
            {
                solutionFolder = SearchSolution("Assets");
            }

            return solutionFolder;
        }

        static string ExePath()
        {
            string path = Path.GetDirectoryName(GetSolutionFilePath());
            path = Path.GetFullPath(path);
            path = Path.Combine(path, "bin", "Debug", "net6.0-windows", "ConsoleGen");
            
            if (IsWindows) path += ".exe";
            
            return path;
        }

        [MenuItem("Code Gen/Compile Run")]
        public static void RunCompilation()
        {
            var solution = GetSolutionFilePath();

            // var path = "";
            // var p = new Process();
            // p.StartInfo.FileName = "cmd.exe";
            // p.StartInfo.Arguments =
            //     @"/c """"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"""" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe";
            // Debug.Log("args-----=> " + p.StartInfo.Arguments);
            // p.StartInfo.CreateNoWindow = true;
            // p.StartInfo.RedirectStandardError = true;
            // p.StartInfo.RedirectStandardOutput = true;
            // p.StartInfo.RedirectStandardInput = false;
            // p.StartInfo.UseShellExecute = false;
            // p.OutputDataReceived += (a, b) =>
            // {
            //     if (b.Data == null) return;
            //     if (File.Exists(b.Data))
            //     {
            //         path = b.Data;
            //     }
            //
            //     Debug.Log(b.Data);
            // };
            // p.ErrorDataReceived += (a, b) =>
            // {
            //     if (b.Data == null) return;
            //     Debug.LogError(b.Data);
            // };
            // p.Start();
            // p.BeginErrorReadLine();
            // p.BeginOutputReadLine();
            // p.WaitForExit();

            RunProcessAndReadLogs(DotnetExecutablePath, "msbuild -t:restore " + solution, null);
            RunProcessAndReadLogs(DotnetExecutablePath, "msbuild " + solution, null);
        }
    }
}

#endif