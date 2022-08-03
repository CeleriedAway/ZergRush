#if UNITY_EDITOR

using System;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        [MenuItem("Code Gen/Run CodeGen #&c")]
        public static void GenCode()
        {
            GenerateInner(includeAssemblies);
            AssetDatabase.Refresh();
        }

        [MenuItem("Code Gen/Run CodeGen Experimental#&c")]
        public static void GenCodeExperimental()
        {
            var path = ExePath();
            if (File.Exists(path) == false)
            {
                RunCompilation();
                if (File.Exists(path) == false)
                {
                    Debug.LogError("compilation did not produce exe");
                    return;
                }
            }
            RunProcessAndReadLogs(path,
                $" {string.Join(' ', includeAssemblies)}", 
                @"Packages\ZergRush\Assets\ZergRush\UnityTools\CodeGen\Console~\bin\Debug\net6.0-windows");
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
                si.WorkingDirectory = dir;
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
            var d = Path.GetDirectoryName(GetSolutionFilePath());
            return Path.Combine(d, "bin", "Debug", "net6.0-windows", "ConsoleGen.exe");
        }

        [MenuItem("Code Gen/Compile Run")]
        public static void RunCompilation()
        {
            var solution = GetSolutionFilePath();
            var frameworkVersions = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "Windows",
                "Microsoft.NET", "Framework64");
            var installedVersions = Directory.GetDirectories(frameworkVersions).Where(x => x.Contains("v"));
            List<string> compilers = new List<string>();

            foreach (var frameworkVersion in installedVersions)
            {
                var compilerFile = Path.Combine(frameworkVersion, "msbuild.exe");
                if (File.Exists(compilerFile))
                {
                    compilers.Add(compilerFile);
                    Debug.Log(compilerFile);
                }
            }
            RunProcessAndReadLogs("cmd.exe", "/C " + compilers.Last() + " " + solution, null);
        }
    }
}

#endif