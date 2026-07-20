#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ZergRush.CodeGen
{
    /// <summary>
    /// Unity-side bridge for the source-local CodeGen CLI.
    /// The generator itself stays in Tools~ so Unity never imports its Roslyn dependencies.
    /// </summary>
    public static class CodeGenerationEditorExtension
    {
        const string packageName = "com.celeriedaway.zergrush";
        const string cliProjectPath = "Runtime/ZergRush.CodeGen/src/ZergRush.CodeGen.Cli/ZergRush.CodeGen.Cli.csproj";
        const string reactiveProjectPath = "Runtime/ZergRush.Reactive/src/ZergRush.Reactive/ZergRush.Reactive.csproj";
        const string buildPath = "Runtime/ZergRush.CodeGen/Tools~/.build";

        [MenuItem("Code Gen/Build Local CLI")]
        public static void BuildLocalCli()
        {
            try
            {
                var cli = BuildCli();
                Debug.Log($"CodeGen CLI built at {cli}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MenuItem("Code Gen/Run Source CodeGen")]
        public static void RunSourceCodeGen()
        {
            try
            {
                var cli = BuildCli();
                var outputDirectory = Path.Combine(Application.dataPath, "ZergRushGenerated");
                var sourceFiles = GetProjectSourceFiles(outputDirectory).ToArray();
                if (sourceFiles.Length == 0)
                    throw new InvalidOperationException("No C# source files were found under Assets.");

                Directory.CreateDirectory(outputDirectory);
                var arguments = new StringBuilder();
                arguments.Append(Quote(cli));
                arguments.Append(" --generate ");
                arguments.Append(Quote(outputDirectory));
                foreach (var sourceFile in sourceFiles)
                {
                    arguments.Append(' ');
                    arguments.Append(Quote(sourceFile));
                }

                RunProcess("dotnet", arguments.ToString(), Application.dataPath);
                AssetDatabase.Refresh();
                Debug.Log($"CodeGen generated source into {outputDirectory}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static string BuildCli()
        {
            var packageRoot = GetPackageRoot();
            var cliProject = Path.Combine(packageRoot, cliProjectPath);
            var reactiveProject = Path.Combine(packageRoot, reactiveProjectPath);
            var buildRoot = Path.Combine(packageRoot, buildPath);

            if (!File.Exists(cliProject))
                throw new FileNotFoundException("The local CodeGen CLI project was not found.", cliProject);
            if (!File.Exists(reactiveProject))
                throw new FileNotFoundException("The local Reactive project was not found.", reactiveProject);

            EditorUtility.DisplayProgressBar("ZergRush CodeGen", "Building local CLI", 0.25f);
            Directory.CreateDirectory(buildRoot);
            var arguments = string.Join(" ", new[]
            {
                "build",
                Quote(cliProject),
                "-c Debug",
                "-p:ZergRushReactiveProjectPath=" + Quote(reactiveProject),
                "-p:ZergRushUnityBuildRoot=" + Quote(buildRoot)
            });
            RunProcess("dotnet", arguments, packageRoot);

            var cliAssembly = Path.Combine(buildRoot, "bin", "ZergRush.CodeGen.Cli", "Debug", "net10.0", "ZergRush.CodeGen.Cli.dll");
            if (!File.Exists(cliAssembly))
                throw new FileNotFoundException("The local CodeGen CLI build completed without producing its assembly.", cliAssembly);

            return cliAssembly;
        }

        static string GetPackageRoot()
        {
            var package = PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => p.name == packageName);
            if (package == null || string.IsNullOrEmpty(package.resolvedPath))
                throw new InvalidOperationException($"Unity package '{packageName}' is not installed.");

            return package.resolvedPath;
        }

        static System.Collections.Generic.IEnumerable<string> GetProjectSourceFiles(string generatedDirectory)
        {
            var normalizedGeneratedDirectory = Path.GetFullPath(generatedDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            return CompilationPipeline.GetAssemblies()
                .SelectMany(assembly => assembly.sourceFiles)
                .Where(File.Exists)
                .Select(Path.GetFullPath)
                .Where(path => path.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.StartsWith(normalizedGeneratedDirectory, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        static void RunProcess(string fileName, string arguments, string workingDirectory)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(output))
                    Debug.Log(output);
                if (!string.IsNullOrWhiteSpace(error))
                    Debug.LogWarning(error);
                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"'{fileName} {arguments}' failed with exit code {process.ExitCode}.");
            }
        }

        static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
#endif
