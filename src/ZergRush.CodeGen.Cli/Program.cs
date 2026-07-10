using ZergRush.CodeGen;

string? generationOutput = null;
var inputs = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--generate")
    {
        if (++i >= args.Length) throw new ArgumentException("--generate requires an output directory.");
        generationOutput = Path.GetFullPath(args[i]);
        continue;
    }

    inputs.Add(Path.GetFullPath(args[i]));
}

if (inputs.Count == 0)
{
    inputs.Add(
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "packages", "com.celeriedaway.zergrush", "Samples~", "CodeGenBasics", "CodeGenSamples.cs")));
}

var parser = new ZRCodeParser();
var types = parser.ParseInputs(inputs);

if (generationOutput != null)
{
    foreach (var type in types)
    {
        type.TargetFolder = new ZRTargetFolderInfo
        {
            Folder = generationOutput,
            Inheritable = true,
            Priority = type.TargetFolder?.Priority ?? 1
        };
    }

    CodeGen.Gen(types, generationOutput);
    Console.WriteLine($"Generated {Directory.EnumerateFiles(generationOutput, "*.cs").Count()} files in {generationOutput}");
    return;
}

Console.WriteLine($"Parsed types: {types.Count}");
foreach (var type in types)
{
    Console.WriteLine($"{type.Kind} {type.FullName}");
    Console.WriteLine($"  Flags: {type.Flags}");
    Console.WriteLine($"  Options: {type.Options}");
    if (type.TargetFolder != null)
    {
        Console.WriteLine($"  TargetFolder: {type.TargetFolder.Folder} priority={type.TargetFolder.Priority} inheritable={type.TargetFolder.Inheritable}");
    }
    if (type.BaseType != null)
    {
        Console.WriteLine($"  Base: {type.BaseType.FullName}");
    }
    if (type.GenericDefinition != null)
    {
        Console.WriteLine($"  GenericDefinition: {type.GenericDefinition.FullName}");
    }
    if (type.ChildTypes.Count > 0)
    {
        Console.WriteLine($"  ChildTypes: {string.Join(", ", type.ChildTypes.Select(child => child.FullName))}");
    }
    Console.WriteLine($"  DataMembers: {type.DataMembers.Count}");
    foreach (var data in type.DataMembers)
    {
        var wrappers = data.WrapperTypes.Count == 0 ? "None" : string.Join(" -> ", data.WrapperTypes);
        Console.WriteLine($"    {data.Kind} {data.Name}: {data.Type?.FullName ?? "<unknown>"} access={data.Access}");
        Console.WriteLine($"      Declared: {data.DeclaredType?.WrittenName ?? data.DeclaredType?.FullName ?? "<unknown>"} wrappers={wrappers}");
        Console.WriteLine($"      Include={data.IncludeFlags} Ignore={data.IgnoreFlags} Options={data.Options} ReadOnly={data.IsReadOnly}");
    }
}
