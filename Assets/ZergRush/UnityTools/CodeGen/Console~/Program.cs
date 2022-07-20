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


class Programm
{
    public static readonly string[] PROJECT_NAMES = {"Assembly-CSharp.csproj", "ZergRush.Core.csproj"};

    public static void Main()
    {
        var tries = 0;
        var path = ".";
        const int triesMax = 30;
        
        while (tries < triesMax)
        {
            var strings = Directory.GetFiles(path);
            if (strings.Any(f => f.EndsWith(PROJECT_NAMES[0]))) break;
            path += Path.DirectorySeparatorChar + "..";
            tries++;
        }

        if (tries >= triesMax)
        {
            throw new ZergRushException();
        }
        
        SyntaxAnalizeStuff(path, PROJECT_NAMES);
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
        var trees = projectName.SelectMany(p =>
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
            return allFilePaths.ConvertAll(ExtractSyntaxTree);
        });
        // var references = apiProject.SelectMany(p => p.MetadataReferences.OfType<PortableExecutableReference>()).ToList();

        var pruned = trees.ConvertAll(TypeReader.PruneTree);
        pruned.ForEach((t) =>
        {
            Console.WriteLine(t.ToString());
            Console.WriteLine(t.FilePath);
        });
        var references = allReferencePaths
            .ConvertAll((rPath) => MetadataReference.CreateFromFile(rPath));
        var allProjectReferencePaths = allProjectReference
            .ConvertAll((rPath) => @$"{projectPath}\{TypeReader.GetOutPath(rPath)}")
            .Where((rPath) =>
            {
                if (File.Exists(rPath)) return true;
                Console.WriteLine($"No such file {rPath}");
                return false;
            }).ToList();
        references.AddRange(allProjectReferencePaths
            .ConvertAll((rPath) => MetadataReference.CreateFromFile(Path.GetFullPath(rPath))));

        
        //Throw Syntax Structure is incorrect so i'm reparsing tree //todo: fix and remove   
        pruned = pruned.ConvertAll((tree) => SyntaxFactory.ParseSyntaxTree(tree.ToString()).WithFilePath(tree.FilePath));
        // var references = allReferencePaths.ConvertAll((rPath) => MetadataReference.CreateFromFile(rPath));
        var compilation = CSharpCompilation.Create("assembly", pruned, references);

        var assembly = Compile(compilation);
        if (assembly == null)
            throw new NullReferenceException("Assembly is null");


        CodeGen.ConsoleGen(new List<Assembly>
        {
            assembly
        }, false);
    }

    private static Assembly? Compile(Compilation compilation)
    {
        using (var ms = new MemoryStream())
        {
            compilation =
                compilation.WithOptions(compilation.Options.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var diagnostic in failures)
                {
                    Console.Error.WriteLine($"Failed to compile code '{diagnostic.Id}'! : {diagnostic.GetMessage()}");
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
        var tree = SyntaxFactory.ParseSyntaxTree(System.IO.File.ReadAllText(filePath));
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