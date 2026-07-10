using ZergRush.CodeGen;

namespace ZergRush.CodeGen.Tests;

public sealed class CodeGenSamplesParserTests
{
    [Fact]
    public void CodeGenSamples_parses_current_generator_input_model()
    {
        var types = ParseCodeGenSamples();

        Assert.Equal(15, types.Count);

        var sample = FindType(types, "ZergRush.Samples.CodeGenSamples");
        Assert.Equal(ZRTypeKind.Class, sample.Kind);
        Assert.Equal(GenTaskFlags.PolymorphicDataPack, sample.Flags);
        Assert.Equal(33, sample.DataMembers.Count);
        Assert.Contains(sample.ChildTypes, child => child.FullName == "ZergRush.Samples.Ancestor");
        Assert.NotNull(sample.TargetFolder);
        Assert.EndsWith(
            Path.Combine("Samples~", "CodeGenBasics", "x_generated"),
            sample.TargetFolder!.Folder,
            StringComparison.OrdinalIgnoreCase);

        Assert.Null(FindMemberOrNull(sample, "stringPropWithoutTagNotIncluded"));
        Assert.Equal(GenTaskFlags.All, FindMember(sample, "someTempIgnoredField").IgnoreFlags);
        Assert.Equal(ZRDataOption.CanBeNull, FindMember(sample, "stringFieldThatCanBeNull").Options);

        AssertMember(sample, "reactiveValue", "ZergRush.Samples.OtherData", "reactiveValue.value", FieldWrapperType.Cell);
        AssertMember(sample, "reactiveNullablePrimitive", "int", "reactiveNullablePrimitive.value", FieldWrapperType.Cell, FieldWrapperType.Nullable);
        AssertMember(sample, "nullablePrimitive", "int", "nullablePrimitive", FieldWrapperType.Nullable);
        AssertMember(
            sample,
            "nestedReactiveNullablePrimitive",
            "int",
            "nestedReactiveNullablePrimitive.value.value.value",
            FieldWrapperType.Cell,
            FieldWrapperType.Cell,
            FieldWrapperType.Cell,
            FieldWrapperType.Nullable);
    }

    [Fact]
    public void CodeGenSamples_registers_constructed_generic_types_with_data_members()
    {
        var types = ParseCodeGenSamples();

        var generic = FindType(types, "ZergRush.Samples.TestGeneric<int>");

        Assert.True(generic.Options.HasFlag(ZRTypeOption.ConstructedGeneric));
        Assert.True(generic.Options.HasFlag(ZRTypeOption.External));
        Assert.Equal("ZergRush.Samples.TestGeneric<T>", generic.GenericDefinition?.FullName);
        Assert.Equal(4, generic.DataMembers.Count);

        AssertMember(generic, "value", "int", "value");
        AssertMember(generic, "values", "System.Collections.Generic.List<int>", "values");
        AssertMember(generic, "valuesByName", "System.Collections.Generic.Dictionary<string, int>", "valuesByName");
        AssertMember(generic, "reactiveValue", "int", "reactiveValue.value", FieldWrapperType.Cell);
    }

    static IReadOnlyList<ZRType> ParseCodeGenSamples()
    {
        var parser = new ZRCodeParser();
        return parser.ParseInputs([CodeGenSamplesPath()]);
    }

    static string CodeGenSamplesPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "packages",
                "com.celeriedaway.zergrush",
                "Samples~",
                "CodeGenBasics",
                "CodeGenSamples.cs");

            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }

        throw new FileNotFoundException("Could not find CodeGenSamples.cs from test output directory.");
    }

    static ZRType FindType(IReadOnlyList<ZRType> types, string fullName)
    {
        var type = types.SingleOrDefault(t => t.FullName == fullName);
        Assert.NotNull(type);
        return type;
    }

    static ZRData FindMember(ZRType type, string name)
    {
        var member = FindMemberOrNull(type, name);
        Assert.NotNull(member);
        return member;
    }

    static ZRData? FindMemberOrNull(ZRType type, string name)
    {
        return type.DataMembers.SingleOrDefault(member => member.Name == name);
    }

    static void AssertMember(ZRType type, string name, string fullTypeName, string access, params FieldWrapperType[] wrappers)
    {
        var member = FindMember(type, name);
        Assert.Equal(fullTypeName, member.Type?.FullName);
        Assert.Equal(access, member.Access);
        Assert.Equal(wrappers, member.WrapperTypes);
    }
}
