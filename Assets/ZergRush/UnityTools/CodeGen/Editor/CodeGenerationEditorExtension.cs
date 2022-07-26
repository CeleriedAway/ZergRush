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

        [MenuItem("Code Gen/Run CodeGen Experimental#&c")]
        public static void GenCodeExperimental()
        {
            Process p = new Process();
            var oh = new OutputHandler();
            oh.Setup(p);
            var si = p.StartInfo;
            si.FileName =
                @"Packages\ZergRush\Assets\ZergRush\UnityTools\CodeGen\Console~\bin\Debug\net6.0-windows\ConsoleGen.exe";
            si.Arguments = $" {string.Join(' ', includeAssemblies)}";

            si.WorkingDirectory =
                @"Packages\ZergRush\Assets\ZergRush\UnityTools\CodeGen\Console~\bin\Debug\net6.0-windows";
            p.Start();
            oh.Start();
            p.WaitForExit();
            oh.Output(
                (sb) => Debug.Log(string.Join('\n', sb)),
                (sbe) => Debug.LogError(string.Join('\n', sbe)));
        }


        public static void GenerateInner(List<string> assemblies, bool onlyStubs = false)
        {
            Debug.Log("GenCode called");
            CodeGen.Gen(assemblies, onlyStubs);
        }
    }

    public class OutputHandler
    {
        private Process process;
        List<string> sb = new(), sbe = new();

        public void Setup(Process p)
        {
            process = p;
            var si = p.StartInfo;
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            p.ErrorDataReceived += (_, e) => { sbe.Add(e.Data); };
            p.OutputDataReceived += (_, e) => { sb.Add(e.Data); };
        }

        public void Start()
        {
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }

        public void Output(Action<List<string>> outHandler, Action<List<string>> errHandler)
        {
            outHandler(sb);
            errHandler(sbe);
        }
    }

    public class Builder
    {
        private static string SolutionFileExtention = ".sln";
        private static string BuildFolderName = "AutoBuild";

        private static string GetProjectPath()
        {
            return Application.dataPath.Replace("/Assets", "");
        }

        private static string[] GetSolutionFilePath()
        {
            var solutionFolder = Application.dataPath.Replace("/Assets", @"\Packages\ZergRush\Assets\ZergRush\UnityTools\CodeGen\Console~");
            var filesInDirectory = Directory.GetFiles(solutionFolder)
                .Where(item => Path.GetExtension(item) == SolutionFileExtention).ToArray();
            if (filesInDirectory.Length <= 0)
            {
                throw new System.Exception("This project not contain solution file (*.sln) to run compilation!");
            }

            return filesInDirectory;
        }

        private static string CreateAndGetBUildFolder()
        {
            var buildPath = Path.Combine(GetProjectPath(), BuildFolderName);
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            return buildPath;
        }


        [MenuItem("Compilation/Run")]
        public static void RunCompilation()
        {
            var solutions = GetSolutionFilePath();
            var buildFolder = CreateAndGetBUildFolder();

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

            var cmdProcess = new Process();
            var oh = new OutputHandler();
            oh.Setup(cmdProcess);
            cmdProcess.StartInfo.FileName = "cmd.exe";

            cmdProcess.StartInfo.Arguments = "/C " + compilers.Last() + " " + solutions.Last();

            cmdProcess.Start();
            oh.Start();
            cmdProcess.WaitForExit();
            oh.Output(
                (sb) => Debug.Log(string.Join('\n', sb)),
                (sbe) => Debug.LogError(string.Join('\n', sbe)));
        }
    }
}

#endif