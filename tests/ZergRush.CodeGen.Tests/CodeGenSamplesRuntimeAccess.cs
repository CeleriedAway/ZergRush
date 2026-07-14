using ZergRush.ReactiveCore;

namespace ZergRush.Samples;

public partial class CodeGenSamples
{
    internal static CodeGenSamples CreatePopulatedForTests()
    {
        var data = new CodeGenSamples
        {
            intField = 17,
            stringProp = "property",
            stringFieldMustNotBeNull = "required",
            stringFieldThatCanBeNull = "optional",
            externalClass = new ExternalClass { somePublicField = 19 },
            vector = new UnityEngine.Vector3(1, 2, 3),
            otherData = new OtherData { someData = 23 },
            otherData2 = new OtherData { someData = 29 },
            arraysAreOk = new[] { 31, 37 },
            nullablePrimitive = 41,
            nullableStruct = new UnityEngine.Vector3(4, 5, 6),
            nullablePlainStruct = new PlainStruct
            {
                value = 43,
                position = new UnityEngine.Vector3(7, 8, 9)
            },
            nullableStructWithGenerationTags = new TaggedStruct { id = 47, label = "tagged" },
            enumValue = SampleEnum.Second,
            nullableEnumValue = SampleEnum.First
        };

        data.listsOfPrimitivesAreOk.AddRange(new[] { 53, 59 });
        data.listsOfDataAreOk.Add(new OtherData { someData = 61 });
        data.dictsAreOk[67] = new OtherData { someData = 71 };
        data.complexStructuresAreAlsoOk[73] = new List<List<string>>
        {
            new List<string> { "nested", "values" }
        };
        data.listOfNullablePrimitives.AddRange(new int?[] { 79, null, 83 });
        data.dictWithNullableValues["present"] = 89;
        data.dictWithNullableValues["missing"] = null;

        data.genericWithPrimitive.value = 97;
        data.genericWithPrimitive.values.AddRange(new[] { 101, 103 });
        data.genericWithPrimitive.reactiveValue.value = 107;
        data.genericWithPrimitive.valuesByName["value"] = 109;

        var custom = new CustomStruct { id = 113, name = "custom" };
        data.genericWithCustomStruct.value = custom;
        data.genericWithCustomStruct.values.Add(custom);
        data.genericWithCustomStruct.reactiveValue.value = custom;
        data.genericWithCustomStruct.valuesByName["value"] = custom;

        data.reactiveValue.value.someData = 127;
        data.nullableReactiveValue.value = new OtherData { someData = 131 };
        data.reactiveNullablePrimitive.value = 137;
        data.reactiveNullableStruct.value = new UnityEngine.Vector3(10, 11, 12);
        data.reactiveNullableStructWithGenerationTags.value = new TaggedStruct { id = 139, label = "reactive" };
        data.nestedReactiveNullablePrimitive.value.value.value = 149;
        data.reactiveCollections.Add(151);
        data.reactiveCollections.Add(157);
        data.ancestorArray.Add(new Ancestor { fields = 163 });
        
        data.genericAncestorArray.Add(new TestPolyGenericParent { intField = 163 });
        data.genericAncestorArray.Add(new TestGenericAncestor<int>
        {
            intField = 167,
            genericField = 173
        });
        data.genericAncestorArray.Add(new TestGenericAncestor<CodeGenSamples>
        {
            intField = 179,
            genericField = new CodeGenSamples { intField = 181 }
        });
        data.genericAncestorArray.Add(new TestGenericChild
        {
            intField = 191,
            genericField = 193,
            additionalField = 197
        });

        return data;
    }

    internal static void AssertEquivalentForTests(CodeGenSamples expected, CodeGenSamples actual)
    {
        var differences = new List<string>();
        expected.CompareCheck(actual, new ZRCompareCheckHelper(), differences.Add);
        Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
        Assert.Equal(
            expected.CalculateHash(new ZRHashHelper()),
            actual.CalculateHash(new ZRHashHelper()));
    }

    internal static void AssertRequiredDefaultsForTests(CodeGenSamples data)
    {
        Assert.NotNull(data.otherData2);
        Assert.NotNull(data.reactiveValue);
        Assert.NotNull(data.reactiveValue.value);
        Assert.NotNull(data.nestedReactiveNullablePrimitive);
        Assert.NotNull(data.nestedReactiveNullablePrimitive.value);
        Assert.NotNull(data.nestedReactiveNullablePrimitive.value.value);
        Assert.NotNull(data.genericWithPrimitive.values);
        Assert.NotNull(data.genericWithPrimitive.reactiveValue);
        Assert.NotNull(data.genericWithPrimitive.valuesByName);
        Assert.NotNull(data.genericWithCustomStruct.values);
        Assert.NotNull(data.genericWithCustomStruct.reactiveValue);
        Assert.NotNull(data.genericWithCustomStruct.valuesByName);
    }

    internal void ClearUpdateIgnoredFieldsForTests()
    {
        dictsAreOk.Clear();
        complexStructuresAreAlsoOk.Clear();
        dictWithNullableValues.Clear();
    }

    internal void ClearNullableFieldsForTests()
    {
        stringFieldThatCanBeNull = null;
        otherData = null;
        nullablePrimitive = null;
        nullableStruct = null;
        nullablePlainStruct = null;
        nullableStructWithGenerationTags = null;
        nullableEnumValue = null;
        nullableReactiveValue.value = null;
        reactiveNullablePrimitive.value = null;
        reactiveNullableStruct.value = null;
        reactiveNullableStructWithGenerationTags.value = null;
        nestedReactiveNullablePrimitive.value.value.value = null;
    }

    internal OtherData OtherData2ForTests => otherData2;
    internal OtherData ReactiveValueForTests => reactiveValue.value;
    internal TestGeneric<int> GenericPrimitiveForTests => genericWithPrimitive;

    internal int IgnoredFieldForTests => someTempIgnoredField;
    internal string ExcludedPropertyForTests => stringPropWithoutTagNotIncluded;

    internal void SetIgnoredAndExcludedMembersForTests(int ignored, string excluded)
    {
        someTempIgnoredField = ignored;
        stringPropWithoutTagNotIncluded = excluded;
    }
}
