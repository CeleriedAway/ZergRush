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
    Console.WriteLine($"  Members: {type.Members.Count}");
    foreach (var member in type.Members)
    {
        Console.WriteLine($"    {member.Kind} {member.Name}: {member.MemberType?.FullName ?? "<unknown>"}");
        var wrappers = member.WrapperTypes.Count == 0 ? "None" : string.Join(" -> ", member.WrapperTypes);
        Console.WriteLine($"      Declared: {member.DeclaredType?.WrittenName ?? member.DeclaredType?.FullName ?? "<unknown>"} wrappers={wrappers}");
        Console.WriteLine($"      Include={member.IncludeFlags} Ignore={member.IgnoreFlags} Options={member.Options} ReadOnly={member.IsReadOnly}");
    }
}
