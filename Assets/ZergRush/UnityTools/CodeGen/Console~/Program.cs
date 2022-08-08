// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using ZergRush;
using ZergRush.CodeGen;

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
            throw new ZergRushException();
        }

        List<string> exclude = new List<string>
        {
            "UnityEngine",
            "ZergRush.Core",
        };

        var projects = Directory.GetFiles(path).Where(f => f.EndsWith(".csproj") && !exclude.Any(e => f.Contains(e)));
        
        SyntaxAnalizeStuff(path, projects.Select(f => Path.GetFileName(f)).ToArray());
    }

    private static void SyntaxAnalizeStuff(string projectPath, string[] projectName)
    {
        Console.WriteLine("\n\nFinding all files \n\nv v v v v v v");
        // var msWorkspace = MSBuildWorkspace.Create();
        //
        // var loader = new MSBuildProjectLoader(msWorkspace);
        //
        // var apiProject = loader.LoadProjectInfoAsync(path).Result;
        //
        // var classesByName = new Dictionary<string, ClassGroup>();

        var allReferencePaths = new HashSet<string>();
        var allProjectReference = new HashSet<string>();
        var files = projectName.SelectMany(p =>
        {
            var (allFilePaths, dlls, projs) =
                TypeReader.FindAllFilesInProject(projectPath, p);
            foreach (var dll in dlls)
            {
                allReferencePaths.Add(dll);
            }
            foreach (var pp in projs)
            {
                allProjectReference.Add(pp);
            }
            ShowEntireList(allFilePaths);
            return allFilePaths;
        }).ToHashSet();

        var trees = new List<SyntaxTree>();
        foreach (var file in files)
        {
            if (File.Exists(file) == false) continue;
            var tree = ExtractSyntaxTree(file);
            trees.Add(tree);
        }

        var pruned = trees.ConvertAll(TypeReader.PruneTree);
        pruned.ForEach((t) =>
        {
            Console.WriteLine(t.FilePath);
        });
        var references = allReferencePaths.Where(f => File.Exists(f)).ConvertAll((rPath) => MetadataReference.CreateFromFile(rPath));
        
        Console.WriteLine(Directory.GetCurrentDirectory());
        var fullPath = Path.GetFullPath("../../../ZergRushCore/bin/debug/net6.0/ZergRushCore.dll");
        references.Add(MetadataReference.CreateFromFile(fullPath));

        Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();
        foreach (var portableExecutableReference in references)
        {
            if (File.Exists(portableExecutableReference.FilePath) == false)
            {
                Console.Error.WriteLine(portableExecutableReference.FilePath + " does not exists");
            }
            try
            {
                var a = AppDomain.CurrentDomain.Load(File.ReadAllBytes(portableExecutableReference.FilePath));
                loadedAssemblies[a.FullName] = a;
            }
            catch (Exception e)
            {
            }
        }
        
        var allProjectReferencePaths = allProjectReference
            .ConvertAll((rPath) => @$"{projectPath}\{TypeReader.GetOutPath(rPath)}")
            .Where((rPath) =>
            {
                if (File.Exists(rPath)) return true;
                Console.WriteLine($"No such file {rPath}");
                return false;
            }).ToList();
        
        //Throw Syntax Structure is incorrect so i'm reparsing tree //todo: fix and remove   
        //pruned = pruned.ConvertAll((tree) => SyntaxFactory.ParseSyntaxTree(tree.ToString()).WithFilePath(tree.FilePath));
        var compilation = CSharpCompilation.Create("assembly", pruned, references);
        var asm = Assembly.GetCallingAssembly();
        compilation = compilation.AddReferences(MetadataReference.CreateFromFile(asm.Location));
        
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            //Console.WriteLine($"!!!!!!!! {args.Name} {args.RequestingAssembly}");
            return loadedAssemblies[args.Name];
        };
        
        var assembly = Compile(compilation);
        if (assembly == null)
            throw new NullReferenceException("Assembly is null");
            
        var ass = AppDomain.CurrentDomain.GetAssemblies().ToList();
        CodeGen.RawGen(new List<Assembly>
        {
            ass.Find(a => a.FullName.StartsWith("ZergRushCore")),
            assembly,
        }, false);
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

                throw new Exception("Unknown error while compiling code !");
            }

            ms.Seek(0, SeekOrigin.Begin);
            var ret = Assembly.Load(ms.ToArray());
            return ret;
        }
    }

    public static SyntaxTree ExtractSyntaxTree(string filePath)
    {
        var tree = SyntaxFactory.ParseSyntaxTree(System.IO.File.ReadAllText(filePath), new CSharpParseOptions(preprocessorSymbols: new string[]
        {
            "UNITY_EDITOR",
            "UNITY_2019_1_OR_NEWER"
        }));
        return tree.WithFilePath(filePath);
    }

    public static void ShowEntireList<T>(List<T> listToShow)
    {
        int i = 0;
        foreach (var value in listToShow)
        {
            Console.WriteLine($"{i}: {value}");
            i++;
        }
    }

    public static void ShowSyntaxTree(SyntaxTree toShow)
    {
        toShow.GetRoot().ToString();
    }
}