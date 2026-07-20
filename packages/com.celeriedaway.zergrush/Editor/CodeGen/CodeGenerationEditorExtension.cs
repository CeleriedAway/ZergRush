#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
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
        const string buildDirectoryName = "ZergRush.CodeGen.Cli";

        public static string BuildCli()
        {
            var packageRoot = GetPackageRoot();
            var cliProject = Path.Combine(packageRoot, cliProjectPath);
            var reactiveProject = Path.Combine(packageRoot, reactiveProjectPath);
            // Keep generated MSBuild sources outside the package. Some CodeGen projects
            // intentionally compile broad source globs, so an obj folder below the
            // CodeGen checkout can be discovered as input on the next build.
            var projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var buildRoot = Path.Combine(projectRoot, "Temp", buildDirectoryName);

            if (!File.Exists(cliProject))
                throw new FileNotFoundException("The local CodeGen CLI project was not found.", cliProject);
            if (!File.Exists(reactiveProject))
                throw new FileNotFoundException("The local Reactive project was not found.", reactiveProject);

            try
            {
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
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static string GetPackageRoot()
        {
            var package = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .FirstOrDefault(p => p.name == packageName);
            if (package == null || string.IsNullOrEmpty(package.resolvedPath))
                throw new InvalidOperationException($"Unity package '{packageName}' is not installed.");

            return package.resolvedPath;
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
                {
                    var details = string.IsNullOrWhiteSpace(error) ? output : error;
                    throw new InvalidOperationException(
                        $"'{fileName} {arguments}' failed with exit code {process.ExitCode}.\n{Tail(details, 8000)}");
                }
            }
        }

        static string Tail(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return "...\n" + value.Substring(value.Length - maxLength);
        }

        static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
#endif
