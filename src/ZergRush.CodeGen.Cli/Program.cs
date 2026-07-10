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
    foreach (var member in type.Members)
    {
        var data = member.ToData();
        var wrappers = member.WrapperTypes.Count == 0 ? "None" : string.Join(" -> ", member.WrapperTypes);
        Console.WriteLine($"    {member.Kind} {member.Name}: {data.Type.FullName} access={data.Access}");
        Console.WriteLine($"      Declared: {member.DeclaredType?.WrittenName ?? member.DeclaredType?.FullName ?? "<unknown>"} wrappers={wrappers}");
        Console.WriteLine($"      Include={member.IncludeFlags} Ignore={member.IgnoreFlags} Options={data.Options} ReadOnly={member.IsReadOnly}");
    }
}
