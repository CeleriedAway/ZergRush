using ZergRush.ReactiveCore;
using ZergRush.Samples;

namespace ZergRush.CodeGen.Tests;

public partial class CodeGenCoverageSamples
{
    internal static CodeGenCoverageSamples CreateEmptyForTests() => new();

    internal static CodeGenCoverageSamples CreatePopulatedForTests()
    {
        var data = new CodeGenCoverageSamples
        {
            booleanValue = true,
            byteValue = 201,
            shortValue = -1234,
            intValue = 123456,
            longValue = -9876543210,
            floatValue = 1.25f,
            doubleValue = -2.5,
            decimalValue = 123456.789m,
            charValue = 'Z',
            requiredString = "required value",
            nullableString = "nullable value",
            enumValue = SampleEnum.Second,
            nullableEnumValue = SampleEnum.First,
            nullableIntValue = -456,
            plainStruct = new PlainStruct
            {
                value = 17,
                position = new UnityEngine.Vector3(1, 2, 3)
            },
            nullablePlainStruct = new PlainStruct
            {
                value = 19,
                position = new UnityEngine.Vector3(4, 5, 6)
            },
            taggedStruct = new TaggedStruct { id = 23, label = "tagged" },
            nullableTaggedStruct = new TaggedStruct { id = 29, label = "nullable tagged" },
            requiredObject = new OtherData { someData = 31 },
            nullableObject = new OtherData { someData = 37 },
            primitiveArray = new[] { 41, 43, 47 }
        };

        data.primitiveList.AddRange(new[] { 53, 59, 61 });
        data.nullablePrimitiveList.AddRange(new int?[] { 67, null, 71 });
        data.objectList.Add(new OtherData { someData = 73 });
        data.objectList.Add(new OtherData { someData = 79 });
        data.nestedList.Add(new List<string> { "nested", "list" });
        data.nestedList.Add(new List<string>());

        data.primitiveDictionary["one"] = 83;
        data.primitiveDictionary["two"] = 89;
        data.nullablePrimitiveDictionary["present"] = 97;
        data.nullablePrimitiveDictionary["missing"] = null;
        data.objectDictionary["first"] = new OtherData { someData = 101 };
        data.nestedDictionary[107] = new List<List<string>>
        {
            new() { "deep", "value" },
            new()
        };

        data.nullableCell.value = 109;
        data.objectCell.value = new OtherData { someData = 113 };
        data.nestedNullableCell.value.value.value = 127;
        data.reactiveCollection.Add(131);
        data.reactiveCollection.Add(137);

        return data;
    }

    internal static CodeGenCoverageSamples CreateBoundaryForTests()
    {
        var data = CreatePopulatedForTests();
        data.booleanValue = false;
        data.byteValue = byte.MaxValue;
        data.shortValue = short.MinValue;
        data.intValue = int.MinValue;
        data.longValue = long.MaxValue;
        data.floatValue = float.MaxValue;
        data.doubleValue = double.MinValue;
        data.decimalValue = decimal.MinValue;
        data.charValue = '\0';
        data.requiredString = "\"quotes\" \\ slash \n newline \u263A";
        data.nullableString = string.Empty;
        data.enumValue = SampleEnum.None;
        data.nullableEnumValue = SampleEnum.Second;
        data.nullableIntValue = int.MaxValue;
        data.primitiveList.Clear();
        data.nullablePrimitiveList.Clear();
        data.objectList.Clear();
        data.nestedList.Clear();
        data.primitiveArray = Array.Empty<int>();
        data.primitiveDictionary.Clear();
        data.nullablePrimitiveDictionary.Clear();
        data.objectDictionary.Clear();
        data.nestedDictionary.Clear();
        data.reactiveCollection.Clear();
        return data;
    }

    internal static CodeGenCoverageSamples CreateNullForTests()
    {
        var data = CreatePopulatedForTests();
        data.nullableString = null;
        data.nullableObject = null;
        data.nullableEnumValue = null;
        data.nullableIntValue = null;
        data.nullablePlainStruct = null;
        data.nullableTaggedStruct = null;
        data.nullablePrimitiveList.Clear();
        data.nullablePrimitiveList.Add(null);
        data.nullablePrimitiveDictionary["present"] = null;
        data.nullableCell.value = null;
        data.nestedNullableCell.value.value.value = null;
        return data;
    }

    internal static void AssertEquivalentForTests(CodeGenCoverageSamples expected, CodeGenCoverageSamples actual)
    {
        Assert.Equal(expected.booleanValue, actual.booleanValue);
        Assert.Equal(expected.byteValue, actual.byteValue);
        Assert.Equal(expected.shortValue, actual.shortValue);
        Assert.Equal(expected.intValue, actual.intValue);
        Assert.Equal(expected.longValue, actual.longValue);
        Assert.Equal(expected.floatValue, actual.floatValue);
        Assert.Equal(expected.doubleValue, actual.doubleValue);
        Assert.Equal(expected.decimalValue, actual.decimalValue);
        Assert.Equal(expected.charValue, actual.charValue);
        Assert.Equal(expected.requiredString, actual.requiredString);
        Assert.Equal(expected.nullableString, actual.nullableString);
        Assert.Equal(expected.enumValue, actual.enumValue);
        Assert.Equal(expected.nullableEnumValue, actual.nullableEnumValue);
        Assert.Equal(expected.nullableIntValue, actual.nullableIntValue);
        AssertPlainStruct(expected.plainStruct, actual.plainStruct);
        AssertNullablePlainStruct(expected.nullablePlainStruct, actual.nullablePlainStruct);
        AssertTaggedStruct(expected.taggedStruct, actual.taggedStruct);
        AssertNullableTaggedStruct(expected.nullableTaggedStruct, actual.nullableTaggedStruct);
        AssertOtherData(expected.requiredObject, actual.requiredObject);
        AssertNullableOtherData(expected.nullableObject, actual.nullableObject);

        Assert.Equal(expected.primitiveList, actual.primitiveList);
        Assert.Equal(expected.nullablePrimitiveList, actual.nullablePrimitiveList);
        Assert.Equal(expected.primitiveArray, actual.primitiveArray);
        Assert.Equal(expected.reactiveCollection.Count, actual.reactiveCollection.Count);
        for (var i = 0; i < expected.reactiveCollection.Count; i++)
            Assert.Equal(expected.reactiveCollection[i], actual.reactiveCollection[i]);

        Assert.Equal(expected.objectList.Count, actual.objectList.Count);
        for (var i = 0; i < expected.objectList.Count; i++)
            AssertOtherData(expected.objectList[i], actual.objectList[i]);

        Assert.Equal(expected.nestedList.Count, actual.nestedList.Count);
        for (var i = 0; i < expected.nestedList.Count; i++)
            Assert.Equal(expected.nestedList[i], actual.nestedList[i]);

        Assert.Equal(expected.primitiveDictionary, actual.primitiveDictionary);
        Assert.Equal(expected.nullablePrimitiveDictionary, actual.nullablePrimitiveDictionary);
        Assert.Equal(expected.nestedDictionary.Count, actual.nestedDictionary.Count);
        foreach (var pair in expected.nestedDictionary)
            Assert.Equal(pair.Value, actual.nestedDictionary[pair.Key]);

        Assert.Equal(expected.objectDictionary.Count, actual.objectDictionary.Count);
        foreach (var pair in expected.objectDictionary)
            AssertOtherData(pair.Value, actual.objectDictionary[pair.Key]);

        Assert.Equal(expected.nullableCell.value, actual.nullableCell.value);
        AssertOtherData(expected.objectCell.value, actual.objectCell.value);
        Assert.Equal(expected.nestedNullableCell.value.value.value, actual.nestedNullableCell.value.value.value);
    }

    internal static void AssertRequiredDefaultsForTests(CodeGenCoverageSamples data)
    {
        Assert.NotNull(data.requiredString);
        Assert.NotNull(data.requiredObject);
        Assert.NotNull(data.primitiveList);
        Assert.NotNull(data.nullablePrimitiveList);
        Assert.NotNull(data.objectList);
        Assert.NotNull(data.nestedList);
        Assert.NotNull(data.primitiveArray);
        Assert.NotNull(data.primitiveDictionary);
        Assert.NotNull(data.nullablePrimitiveDictionary);
        Assert.NotNull(data.objectDictionary);
        Assert.NotNull(data.nestedDictionary);
        Assert.NotNull(data.objectCell);
        Assert.NotNull(data.objectCell.value);
        Assert.NotNull(data.nestedNullableCell);
        Assert.NotNull(data.nestedNullableCell.value);
        Assert.NotNull(data.nestedNullableCell.value.value);
    }

    static void AssertOtherData(OtherData expected, OtherData actual) => Assert.Equal(expected.someData, actual.someData);

    static void AssertNullableOtherData(OtherData? expected, OtherData? actual)
    {
        if (expected is null || actual is null)
        {
            Assert.Equal(expected, actual);
            return;
        }

        AssertOtherData(expected, actual);
    }

    static void AssertPlainStruct(PlainStruct expected, PlainStruct actual)
    {
        Assert.Equal(expected.value, actual.value);
        Assert.Equal(expected.position.x, actual.position.x);
        Assert.Equal(expected.position.y, actual.position.y);
        Assert.Equal(expected.position.z, actual.position.z);
    }

    static void AssertNullablePlainStruct(PlainStruct? expected, PlainStruct? actual)
    {
        Assert.Equal(expected.HasValue, actual.HasValue);
        if (expected.HasValue && actual.HasValue) AssertPlainStruct(expected.Value, actual.Value);
    }

    static void AssertTaggedStruct(TaggedStruct expected, TaggedStruct actual)
    {
        Assert.Equal(expected.id, actual.id);
        Assert.Equal(expected.label, actual.label);
    }

    static void AssertNullableTaggedStruct(TaggedStruct? expected, TaggedStruct? actual)
    {
        Assert.Equal(expected.HasValue, actual.HasValue);
        if (expected.HasValue && actual.HasValue) AssertTaggedStruct(expected.Value, actual.Value);
    }
}
