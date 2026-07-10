using ZergRush.Samples;

namespace ZergRush.CodeGen.Tests;

public sealed class GeneratedCodeBehaviorTests
{
    [Fact]
    public void UpdateFrom_copies_generated_data_members()
    {
        var source = new OtherData { someData = 42 };
        var destination = new OtherData { someData = -1 };

        destination.UpdateFrom(source, new ZRUpdateFromHelper());

        Assert.Equal(42, destination.someData);
        Assert.Equal(
            source.CalculateHash(new ZRHashHelper()),
            destination.CalculateHash(new ZRHashHelper()));
    }

    [Fact]
    public void Binary_serialization_round_trips_generated_data()
    {
        var source = new OtherData { someData = 73 };

        var restored = source.WriteToByteArray().Read<OtherData>();

        Assert.NotSame(source, restored);
        Assert.Equal(source.someData, restored.someData);
    }

    [Fact]
    public void Json_serialization_round_trips_generated_data()
    {
        var source = new OtherData { someData = 91 };

        var restored = source.WriteToJsonString().ReadFromJson<OtherData>();

        Assert.NotSame(source, restored);
        Assert.Equal(source.someData, restored.someData);
    }

    [Fact]
    public void Polymorphic_factory_constructs_the_requested_generated_type()
    {
        var created = CodeGenSamples.CreatePolymorphic((ushort)CodeGenSamples.Types.Ancestor);

        var ancestor = Assert.IsType<Ancestor>(created);
        Assert.Equal((ushort)CodeGenSamples.Types.Ancestor, ancestor.GetClassId());
        Assert.IsType<Ancestor>(ancestor.NewInst());
    }

    [Fact]
    public void Unregistered_generic_hierarchy_instance_throws_clearly()
    {
        var exception = Assert.Throws<NotSupportedException>(() => new TestGenericAncestor<long>());

        Assert.Contains("TestGenericAncestor", exception.Message);
        Assert.Contains("not registered", exception.Message);
    }

    [Fact]
    public void Dictionary_UpdateFrom_reuses_values_and_deep_copies_new_entries()
    {
        var reusedValue = new OtherData { someData = -1 };
        var destination = new DictionaryUpdateFromSample
        {
            values =
            {
                [1] = reusedValue,
                [3] = new OtherData { someData = 300 }
            }
        };
        var source = new DictionaryUpdateFromSample
        {
            values =
            {
                [1] = new OtherData { someData = 100 },
                [2] = new OtherData { someData = 200 }
            }
        };
        var destinationDictionary = destination.values;

        destination.UpdateFrom(source, new ZRUpdateFromHelper());

        Assert.Same(destinationDictionary, destination.values);
        Assert.Same(reusedValue, destination.values[1]);
        Assert.Equal(100, destination.values[1].someData);
        Assert.NotSame(source.values[2], destination.values[2]);
        Assert.Equal(200, destination.values[2].someData);
        Assert.False(destination.values.ContainsKey(3));
    }

    [Fact]
    public void Generated_constructor_initializes_required_wrappers_and_values()
    {
        CodeGenSamples.AssertRequiredDefaultsForTests(new CodeGenSamples());
    }

    [Fact]
    public void Full_sample_binary_serialization_round_trips()
    {
        var source = CodeGenSamples.CreatePopulatedForTests();

        var restored = source.WriteToByteArray().Read<CodeGenSamples>();

        CodeGenSamples.AssertEquivalentForTests(source, restored);
    }

    [Fact]
    public void Full_sample_json_serialization_round_trips()
    {
        var source = CodeGenSamples.CreatePopulatedForTests();

        var restored = source.WriteToJsonString().ReadFromJson<CodeGenSamples>();

        CodeGenSamples.AssertEquivalentForTests(source, restored);
    }

    [Fact]
    public void Full_sample_UpdateFrom_reuses_prepared_instances()
    {
        var source = CodeGenSamples.CreatePopulatedForTests();
        source.ClearUpdateIgnoredFieldsForTests();
        var destination = new CodeGenSamples();
        var otherData2 = destination.OtherData2ForTests;
        var reactiveValue = destination.ReactiveValueForTests;
        var genericPrimitive = destination.GenericPrimitiveForTests;
        var genericValues = genericPrimitive.values;
        var genericDictionary = genericPrimitive.valuesByName;

        destination.UpdateFrom(source, new ZRUpdateFromHelper());

        CodeGenSamples.AssertEquivalentForTests(source, destination);
        Assert.Same(otherData2, destination.OtherData2ForTests);
        Assert.Same(reactiveValue, destination.ReactiveValueForTests);
        Assert.Same(genericPrimitive, destination.GenericPrimitiveForTests);
        Assert.Same(genericValues, destination.GenericPrimitiveForTests.values);
        Assert.Same(genericDictionary, destination.GenericPrimitiveForTests.valuesByName);
    }

    [Fact]
    public void Full_sample_preserves_null_values_across_binary_and_json()
    {
        var source = CodeGenSamples.CreatePopulatedForTests();
        source.ClearNullableFieldsForTests();

        var binary = source.WriteToByteArray().Read<CodeGenSamples>();
        var json = source.WriteToJsonString().ReadFromJson<CodeGenSamples>();

        CodeGenSamples.AssertEquivalentForTests(source, binary);
        CodeGenSamples.AssertEquivalentForTests(source, json);
    }
}
