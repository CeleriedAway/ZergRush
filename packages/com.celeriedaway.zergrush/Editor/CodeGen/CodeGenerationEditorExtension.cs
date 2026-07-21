#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        const string buildFingerprintFileName = "source.fingerprint";
        static readonly string[] buildInputPaths =
        {
            "Runtime/ZergRush.CodeGen/Directory.Build.props",
            "Runtime/ZergRush.CodeGen/Directory.Build.targets",
            "Runtime/ZergRush.CodeGen/global.json",
            "Runtime/ZergRush.CodeGen/src",
            "Runtime/ZergRush.CodeGen/Tools~",
            "Runtime/ZergRush.CodeGen/Editor",
            "Runtime/ZergRush.CodeGen/Runtime",
            "Runtime/ZergRush.Reactive/Directory.Build.props",
            "Runtime/ZergRush.Reactive/Directory.Build.targets",
            "Runtime/ZergRush.Reactive/global.json",
            "Runtime/ZergRush.Reactive/src",
            "Runtime/ZergRush.Reactive/Runtime"
        };
        static readonly string[] buildInputExtensions =
        {
            ".cs", ".csproj", ".props", ".targets", ".json", ".config", ".editorconfig", ".resx"
        };
        static readonly string[] localBuildArtifactPaths =
        {
            "Runtime/ZergRush.CodeGen/src/ZergRush.CodeGen/bin",
            "Runtime/ZergRush.CodeGen/src/ZergRush.CodeGen/obj",
            "Runtime/ZergRush.CodeGen/src/ZergRush.CodeGen.Core/bin",
            "Runtime/ZergRush.CodeGen/src/ZergRush.CodeGen.Core/obj",
            "Runtime/ZergRush.CodeGen/src/ZergRush.CodeGen.Cli/bin",
            "Runtime/ZergRush.CodeGen/src/ZergRush.CodeGen.Cli/obj",
            "Runtime/ZergRush.Reactive/src/ZergRush.Reactive/bin",
            "Runtime/ZergRush.Reactive/src/ZergRush.Reactive/obj"
        };

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
            var cliAssembly = Path.Combine(buildRoot, "bin", "ZergRush.CodeGen.Cli", "Debug", "net10.0", "ZergRush.CodeGen.Cli.dll");
            var fingerprintPath = Path.Combine(buildRoot, buildFingerprintFileName);

            if (!File.Exists(cliProject))
                throw new FileNotFoundException("The local CodeGen CLI project was not found.", cliProject);
            if (!File.Exists(reactiveProject))
                throw new FileNotFoundException("The local Reactive project was not found.", reactiveProject);

            Directory.CreateDirectory(buildRoot);
            var sourceFingerprint = ComputeBuildFingerprint(packageRoot);
            if (File.Exists(cliAssembly) &&
                File.Exists(fingerprintPath) &&
                File.ReadAllText(fingerprintPath) == sourceFingerprint)
            {
                CleanupLocalBuildArtifacts(packageRoot);
                Debug.Log("ZergRush CodeGen CLI sources unchanged; reusing cached build.");
                return cliAssembly;
            }

            try
            {
                EditorUtility.DisplayProgressBar("ZergRush CodeGen", "Building local CLI", 0.25f);
                var arguments = string.Join(" ", new[]
                {
                    "build",
                    Quote(cliProject),
                    "-c Debug",
                    "-p:ZergRushReactiveProjectPath=" + Quote(reactiveProject),
                    "-p:ZergRushUnityBuildRoot=" + Quote(buildRoot)
                });
                RunProcess("dotnet", arguments, packageRoot);

                if (!File.Exists(cliAssembly))
                    throw new FileNotFoundException("The local CodeGen CLI build completed without producing its assembly.", cliAssembly);

                File.WriteAllText(fingerprintPath, sourceFingerprint);
                return cliAssembly;
            }
            finally
            {
                CleanupLocalBuildArtifacts(packageRoot);
                EditorUtility.ClearProgressBar();
            }
        }

        static string ComputeBuildFingerprint(string packageRoot)
        {
            var files = buildInputPaths
                .Select(path => Path.Combine(packageRoot, path))
                .SelectMany(EnumerateBuildInputFiles)
                .Where(IsBuildInputFile)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

            using (var hash = SHA256.Create())
            {
                var buffer = new byte[81920];
                var separator = new byte[] { 0 };
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(packageRoot, file).Replace('\\', '/');
                    var pathBytes = Encoding.UTF8.GetBytes(relativePath);
                    hash.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);
                    hash.TransformBlock(separator, 0, separator.Length, separator, 0);

                    using (var stream = new FileStream(
                               file, FileMode.Open, FileAccess.Read,
                               FileShare.ReadWrite | FileShare.Delete))
                    {
                        int bytesRead;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            hash.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                    }

                    hash.TransformBlock(separator, 0, separator.Length, separator, 0);
                }

                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return "v1:" + BitConverter.ToString(hash.Hash).Replace("-", string.Empty);
            }
        }

        static IEnumerable<string> EnumerateBuildInputFiles(string path)
        {
            if (File.Exists(path))
                return new[] { path };
            if (Directory.Exists(path))
                return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            return Array.Empty<string>();
        }

        static bool IsBuildInputFile(string path)
        {
            var normalizedPath = path.Replace('\\', '/');
            if (normalizedPath.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalizedPath.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalizedPath.IndexOf("/.git/", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            var extension = Path.GetExtension(path);
            return buildInputExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        static void CleanupLocalBuildArtifacts(string packageRoot)
        {
            foreach (var relativePath in localBuildArtifactPaths)
            {
                var artifactPath = Path.Combine(packageRoot, relativePath);
                if (Directory.Exists(artifactPath))
                    Directory.Delete(artifactPath, true);

                // Unity can create these while dotnet is still producing the folder.
                // Removing the folder alone lets AssetDatabase recreate it from its meta.
                var metaPath = artifactPath + ".meta";
                if (File.Exists(metaPath))
                    File.Delete(metaPath);
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
