using ZergRush.CodeGen;

if (args.Length == 0)
{
    args =
    [
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "packages", "com.celeriedaway.zergrush", "Samples~", "CodeGenBasics", "CodeGenSamples.cs"))
    ];
}

var parser = new ZRCodeParser();
var types = parser.ParseInputs(args);

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
