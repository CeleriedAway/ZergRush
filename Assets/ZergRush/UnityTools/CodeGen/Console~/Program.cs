// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

#if UNITY_EDITOR
#endif


class Programm
{
    public static readonly string[] PROJECT_NAMES =
    {
        "Assembly-CSharp",
        "Assembly-CSharp-firstpass",
        "ZergRush.Core",
    };
    
    private static string ProjectFilePath = string.Empty;

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            args = PROJECT_NAMES;
        }
        args = args.Select(p => p + ".csproj").ToArray();
        var tries = 0;
        var path = ".";
        const int triesMax = 30;

        while (tries < triesMax)
        {
            var strings = Directory.GetFiles(path);
            if (strings.Any(f => f.EndsWith(args[0])))
            {
                ProjectFilePath = Path.GetDirectoryName(Path.GetFullPath(strings.First()));
                break;
            }
            path += Path.DirectorySeparatorChar + "..";
            tries++;
        }

        if (tries >= triesMax)
        {
            throw new InvalidOperationException();
        }

        List<string> exclude = new List<string>
        {
            "UnityEngine",
        };

        var projects = Directory.GetFiles(path).Where(f => f.EndsWith(".csproj") && !exclude.Any(e => f.Contains(e))).ToList();
        projects.RemoveAll(f => f.Contains("HotReload", StringComparison.InvariantCultureIgnoreCase));

        foreach (var project in projects)
        {
            Console.WriteLine(project);
        }

        SyntaxAnalizeStuff(path, projects.Select(f => Path.GetFileName(f)).ToArray());
    }
    private static readonly string[] RuntimeRequiredLibs =
    {
        "mscorlib",
        "System",
        "System.Core"
    };
    
    private const string Runtime = "NetStandard";

    private static void SyntaxAnalizeStuff(string projectPath, string[] projectName)
    {
        var allReferencePaths = new HashSet<string>();
        var allProjectReference = new HashSet<string>();

        var projectDefines = TypeReader.ProjectDefines(Path.Combine(projectPath, PROJECT_NAMES[0] + ".csproj"));
        projectDefines.Add("CONSOLE_GEN");
        var defines = projectDefines.ToArray();
        
        var files = projectName.SelectMany(p =>
        {
            var (allFilePaths, dlls, projs) = TypeReader.FindAllFilesInProject(projectPath, p);
            foreach (var dll in dlls)
            {
                //Console.WriteLine($"dll: {dll}");
                if (dll.Contains("ZergRush") || dll.Contains("CodeGen")) continue;
                allReferencePaths.Add(dll);
            }
            foreach (var pp in projs)
            {

                allProjectReference.Add(pp);
            }
            return allFilePaths;
        }).ToHashSet();

        var trees = new List<SyntaxTree>();
        foreach (var file in files)
        {
            if (file.EndsWith("AssemblyInfo.cs")) continue;
            if (File.Exists(file) == false) continue;
            var tree = ExtractSyntaxTree(file, defines);
            trees.Add(tree);
        }

        var pruned = trees.ConvertAll(TypeReader.PruneTree);
        var references = allReferencePaths.Where(f => File.Exists(f)).Select((rPath) => MetadataReference.CreateFromFile(rPath));        

        Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();
        foreach (var portableExecutableReference in references)
        {
            // if (File.Exists(portableExecutableReference.FilePath) == false)
            // {
            //     Console.Error.WriteLine(portableExecutableReference.FilePath + " does not exists");
            // }
            try
            {
                // var a = AppDomain.CurrentDomain.Load(File.ReadAllBytes(portableExecutableReference.FilePath));
                // loadedAssemblies[a.FullName] = a;
                
                // if (RuntimeRequiredLibs.Any(portableExecutableReference.FilePath.Contains) 
                //     && !portableExecutableReference.FilePath.Contains(Runtime))
                //     continue;

                var a = AppDomain.CurrentDomain.Load(File.ReadAllBytes(portableExecutableReference.FilePath));
                var name = a.FullName.Split(',')[0].Trim().ToLower();
                //Console.WriteLine($"~~~~~~ {name}");
                loadedAssemblies[name] = a;
            }
            catch (Exception e)
            {
            }
        }
        
        var compilation = CSharpCompilation.Create("assembly", pruned, references);
        compilation = compilation.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var shortName = args.Name.Split(',')[0].Trim().ToLower();
            //Console.WriteLine($"!!!!!!!! {shortName}");
            return loadedAssemblies[shortName];
        };
        
        var assembly = Compile(compilation);
        if (assembly == null) throw new NullReferenceException("Assembly is null");

        var cg = assembly.GetTypes().First(t => t.Name == "CodeGen");
        try
        {
            cg.GetMethod("RawGen").Invoke(null, new[] { (object)new List<Assembly> { assembly }, Path.Combine(projectPath, "Assets", "zGenerated"), (object)false });

        }
        catch (Exception e)
        {
            Console.WriteLine(e.InnerException);
            Console.WriteLine(e.InnerException.StackTrace);
        }
    }


    private static string CheckLinkPath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }
        else
        {
            var file = Path.Combine(ProjectFilePath, path);
            if (File.Exists(file))
            {
                return file;
            }
            else
            {
                throw new Exception($"File \"{path}\" cant be found in project path. Current project path is \"{ProjectFilePath}\"");
            }
        }
    }

    private static Assembly? Compile(Compilation compilation)
    {
        using (var ms = new MemoryStream())
        {
            compilation = compilation.WithOptions(compilation.Options.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var diagnostic in failures)
                {
                    Console.Error.WriteLine($"Failed to compile code '{diagnostic.Id}'! : {diagnostic.GetMessage()} " +
                                            $"in\n{diagnostic.Location.SourceTree.FilePath} {diagnostic.Location.SourceSpan.ToString()}");
                }

                Console.ReadLine();
                return null;
            }

            ms.Seek(0, SeekOrigin.Begin);
            var ret = Assembly.Load(ms.ToArray());
            return ret;
        }
    }

    public static SyntaxTree ExtractSyntaxTree(string filePath, string[] defines)
    {
        if (filePath.Contains("LogSink.cs")) defines = new string[]{};
        var tree = SyntaxFactory.ParseSyntaxTree(System.IO.File.ReadAllText(filePath), new CSharpParseOptions(preprocessorSymbols: defines));
        return tree.WithFilePath(filePath);
    }

    public static void ShowSyntaxTree(SyntaxTree toShow)
    {
        toShow.GetRoot().ToString();
    }
}