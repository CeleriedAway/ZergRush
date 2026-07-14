using ZergRush;
using ZergRush.Samples;

namespace ZergRush.CodeGen.Tests;

public sealed class CodeGenCoverageTests
{
    public static IEnumerable<object[]> Scenarios()
    {
        yield return ["default", CodeGenCoverageSamples.CreateEmptyForTests()];
        yield return ["populated", CodeGenCoverageSamples.CreatePopulatedForTests()];
        yield return ["nulls", CodeGenCoverageSamples.CreateNullForTests()];
        yield return ["boundaries-and-empty-containers", CodeGenCoverageSamples.CreateBoundaryForTests()];
    }

    public static IEnumerable<object[]> Mutations()
    {
        yield return [new MutationCase("booleanValue", x => x.booleanValue = !x.booleanValue)];
        yield return [new MutationCase("byteValue", x => x.byteValue++)];
        yield return [new MutationCase("shortValue", x => x.shortValue++)];
        yield return [new MutationCase("intValue", x => x.intValue++)];
        yield return [new MutationCase("longValue", x => x.longValue++)];
        yield return [new MutationCase("floatValue", x => x.floatValue += 1.0f)];
        yield return [new MutationCase("doubleValue", x => x.doubleValue += 1.0)];
        yield return [new MutationCase("decimalValue", x => x.decimalValue += 1.0m)];
        yield return [new MutationCase("charValue", x => x.charValue = 'Y')];
        yield return [new MutationCase("requiredString", x => x.requiredString = "changed")];
        yield return [new MutationCase("nullableString", x => x.nullableString = null)];
        yield return [new MutationCase("enumValue", x => x.enumValue = SampleEnum.First)];
        yield return [new MutationCase("nullableEnumValue", x => x.nullableEnumValue = null)];
        yield return [new MutationCase("nullableIntValue", x => x.nullableIntValue = null)];
        yield return [new MutationCase("plainStruct", x => x.plainStruct.value++)];
        yield return [new MutationCase("nullablePlainStruct", x => x.nullablePlainStruct = null)];
        yield return [new MutationCase("taggedStruct", x => x.taggedStruct.id++)];
        yield return [new MutationCase("nullableTaggedStruct", x => x.nullableTaggedStruct = null)];
        yield return [new MutationCase("requiredObject", x => x.requiredObject.someData++)];
        yield return [new MutationCase("nullableObject", x => x.nullableObject = null)];
        yield return [new MutationCase("primitiveList", x => x.primitiveList[0]++)];
        yield return [new MutationCase("nullablePrimitiveList", x => x.nullablePrimitiveList[0] = null)];
        yield return [new MutationCase("objectList", x => x.objectList[0].someData++)];
        yield return [new MutationCase("nestedList", x => x.nestedList[0][0] = "changed")];
        yield return [new MutationCase("primitiveArray", x => x.primitiveArray[0]++)];
        yield return [new MutationCase("primitiveDictionary", x => x.primitiveDictionary["one"]++)];
        yield return [new MutationCase("nullablePrimitiveDictionary", x => x.nullablePrimitiveDictionary["present"] = null)];
        yield return [new MutationCase("objectDictionary", x => x.objectDictionary["first"].someData++)];
        yield return [new MutationCase("nestedDictionary", x => x.nestedDictionary[107][0][0] = "changed")];
        yield return [new MutationCase("nullableCell", x => x.nullableCell.value = null)];
        yield return [new MutationCase("objectCell", x => x.objectCell.value.someData++)];
        yield return [new MutationCase("nestedNullableCell", x => x.nestedNullableCell.value.value.value = null)];
        yield return [new MutationCase("reactiveCollection", x => x.reactiveCollection[0]++)];
    }

    [Fact]
    public void Generated_constructor_initializes_required_members_without_sharing_mutable_state()
    {
        var first = new CodeGenCoverageSamples();
        var second = new CodeGenCoverageSamples();

        CodeGenCoverageSamples.AssertRequiredDefaultsForTests(first);
        CodeGenCoverageSamples.AssertRequiredDefaultsForTests(second);

        first.primitiveList.Add(1);
        first.primitiveDictionary["first"] = 2;
        first.objectCell.value.someData = 3;

        Assert.Empty(second.primitiveList);
        Assert.Empty(second.primitiveDictionary);
        Assert.NotEqual(first.objectCell.value.someData, second.objectCell.value.someData);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Binary_round_trip_preserves_every_field(string _, CodeGenCoverageSamples source)
    {
        var restored = source.WriteToByteArray().Read<CodeGenCoverageSamples>();

        Assert.NotSame(source, restored);
        CodeGenCoverageSamples.AssertEquivalentForTests(source, restored);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Json_round_trip_preserves_every_field(string _, CodeGenCoverageSamples source)
    {
        var restored = source.WriteToJsonString().ReadFromJson<CodeGenCoverageSamples>();

        Assert.NotSame(source, restored);
        CodeGenCoverageSamples.AssertEquivalentForTests(source, restored);
    }

    [Fact]
    public void Json_round_trip_preserves_escaped_and_unicode_strings()
    {
        var source = CodeGenCoverageSamples.CreateBoundaryForTests();

        var restored = source.WriteToJsonString().ReadFromJson<CodeGenCoverageSamples>();

        Assert.Equal(source.requiredString, restored.requiredString);
        Assert.Equal(source.nullableString, restored.nullableString);
    }

    [Fact]
    public void Json_reader_ignores_unknown_fields_and_keeps_defaults_for_missing_fields()
    {
        var source = CodeGenCoverageSamples.CreatePopulatedForTests();
        var json = source.WriteToJsonString();
        var withUnknown = json.Insert(1, "\"unknownField\":123,");
        var withoutInt = json.Replace($"\"intValue\": {source.intValue}", "\"missingInt\": 0", StringComparison.Ordinal);
        Assert.DoesNotContain("\"intValue\"", withoutInt, StringComparison.Ordinal);

        var unknownRestored = withUnknown.ReadFromJson<CodeGenCoverageSamples>();
        var missingRestored = withoutInt.ReadFromJson<CodeGenCoverageSamples>();

        CodeGenCoverageSamples.AssertEquivalentForTests(source, unknownRestored);
        Assert.Equal(0, missingRestored.intValue);
    }

    [Fact]
    public void UpdateFrom_copies_every_field_and_reuses_prepared_instances()
    {
        var source = CodeGenCoverageSamples.CreatePopulatedForTests();
        var destination = CodeGenCoverageSamples.CreatePopulatedForTests();
        destination.intValue = -1;
        destination.objectList.Add(new ZergRush.Samples.OtherData { someData = 999 });
        destination.primitiveDictionary["removed"] = -1;
        destination.objectDictionary["removed"] = new ZergRush.Samples.OtherData { someData = -1 };

        var requiredObject = destination.requiredObject;
        var nullableObject = destination.nullableObject;
        var primitiveList = destination.primitiveList;
        var objectList = destination.objectList;
        var primitiveDictionary = destination.primitiveDictionary;
        var objectDictionary = destination.objectDictionary;
        var objectCell = destination.objectCell;
        var objectCellValue = destination.objectCell.value;
        var reactiveCollection = destination.reactiveCollection;

        destination.UpdateFrom(source, new ZRUpdateFromHelper());

        CodeGenCoverageSamples.AssertEquivalentForTests(source, destination);
        Assert.Same(requiredObject, destination.requiredObject);
        Assert.Same(nullableObject, destination.nullableObject);
        Assert.Same(primitiveList, destination.primitiveList);
        Assert.Same(objectList, destination.objectList);
        Assert.Same(primitiveDictionary, destination.primitiveDictionary);
        Assert.Same(objectDictionary, destination.objectDictionary);
        Assert.Same(objectCell, destination.objectCell);
        Assert.Same(objectCellValue, destination.objectCell.value);
        Assert.Same(reactiveCollection, destination.reactiveCollection);

        source.requiredObject.someData = 1001;
        source.objectDictionary["first"].someData = 1003;
        source.objectList[0].someData = 1005;

        Assert.NotEqual(source.requiredObject.someData, destination.requiredObject.someData);
        Assert.NotEqual(source.objectDictionary["first"].someData, destination.objectDictionary["first"].someData);
        Assert.NotEqual(source.objectList[0].someData, destination.objectList[0].someData);
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void CalculateHash_changes_when_one_included_field_changes(MutationCase mutation)
    {
        var source = CodeGenCoverageSamples.CreatePopulatedForTests();
        var before = source.CalculateHash(new ZRHashHelper());

        mutation.Apply(source);

        var after = source.CalculateHash(new ZRHashHelper());
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void CalculateHash_is_deterministic_for_equal_values()
    {
        var first = CodeGenCoverageSamples.CreatePopulatedForTests();
        var second = CodeGenCoverageSamples.CreatePopulatedForTests();

        Assert.Equal(first.CalculateHash(new ZRHashHelper()), first.CalculateHash(new ZRHashHelper()));
        Assert.Equal(first.CalculateHash(new ZRHashHelper()), second.CalculateHash(new ZRHashHelper()));
    }

    [Fact]
    public void CompareCheck_prints_nothing_for_equal_values()
    {
        var first = CodeGenCoverageSamples.CreatePopulatedForTests();
        var second = CodeGenCoverageSamples.CreatePopulatedForTests();
        var differences = new List<string>();

        first.CompareCheck(second, new ZRCompareCheckHelper(), differences.Add);

        Assert.Empty(differences);
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void CompareCheck_reports_the_changed_member(MutationCase mutation)
    {
        var expected = CodeGenCoverageSamples.CreatePopulatedForTests();
        var actual = CodeGenCoverageSamples.CreatePopulatedForTests();
        mutation.Apply(actual);
        var differences = new List<string>();

        expected.CompareCheck(actual, new ZRCompareCheckHelper(), differences.Add);

        Assert.NotEmpty(differences);
        Assert.Contains(differences, difference => difference.Contains(mutation.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void CompareCheck_reports_multiple_differences_and_null_transitions()
    {
        var expected = CodeGenCoverageSamples.CreatePopulatedForTests();
        var actual = CodeGenCoverageSamples.CreateNullForTests();
        actual.intValue++;
        actual.requiredString = "different";
        var differences = new List<string>();

        expected.CompareCheck(actual, new ZRCompareCheckHelper(), differences.Add);

        Assert.Contains(differences, difference => difference.Contains("intValue", StringComparison.Ordinal));
        Assert.Contains(differences, difference => difference.Contains("requiredString", StringComparison.Ordinal));
        Assert.Contains(differences, difference => difference.Contains("nullableObject", StringComparison.Ordinal));
        Assert.True(differences.Count >= 3);
    }

    [Fact]
    public void Dictionary_hash_changes_when_a_key_is_added_or_removed()
    {
        var baseline = CodeGenCoverageSamples.CreatePopulatedForTests();
        var added = CodeGenCoverageSamples.CreatePopulatedForTests();
        var removed = CodeGenCoverageSamples.CreatePopulatedForTests();
        var baselineHash = baseline.CalculateHash(new ZRHashHelper());

        added.primitiveDictionary["added"] = 149;
        removed.primitiveDictionary.Remove("one");

        Assert.NotEqual(baselineHash, added.CalculateHash(new ZRHashHelper()));
        Assert.NotEqual(baselineHash, removed.CalculateHash(new ZRHashHelper()));
    }

    [Fact]
    public void CompareCheck_reports_dictionary_key_additions_and_removals()
    {
        var expected = CodeGenCoverageSamples.CreatePopulatedForTests();
        var added = CodeGenCoverageSamples.CreatePopulatedForTests();
        var removed = CodeGenCoverageSamples.CreatePopulatedForTests();
        added.primitiveDictionary["added"] = 151;
        removed.primitiveDictionary.Remove("one");

        var addedDifferences = new List<string>();
        var removedDifferences = new List<string>();
        expected.CompareCheck(added, new ZRCompareCheckHelper(), addedDifferences.Add);
        expected.CompareCheck(removed, new ZRCompareCheckHelper(), removedDifferences.Add);

        Assert.Contains(addedDifferences, difference => difference.Contains("added", StringComparison.Ordinal));
        Assert.Contains(removedDifferences, difference => difference.Contains("one", StringComparison.Ordinal));
    }

    [Fact]
    public void Ignored_and_excluded_members_do_not_participate_in_generated_operations()
    {
        var source = CodeGenSamples.CreatePopulatedForTests();
        source.SetIgnoredAndExcludedMembersForTests(157, "excluded");
        var restored = source.WriteToByteArray().Read<CodeGenSamples>();
        var differences = new List<string>();
        var comparison = CodeGenSamples.CreatePopulatedForTests();

        Assert.Equal(0, restored.IgnoredFieldForTests);
        Assert.Null(restored.ExcludedPropertyForTests);
        Assert.Equal(
            comparison.CalculateHash(new ZRHashHelper()),
            source.CalculateHash(new ZRHashHelper()));

        source.CompareCheck(comparison, new ZRCompareCheckHelper(), differences.Add);
        Assert.DoesNotContain(differences, difference => difference.Contains("someTempIgnoredField", StringComparison.Ordinal));
        Assert.DoesNotContain(differences, difference => difference.Contains("stringPropWithoutTagNotIncluded", StringComparison.Ordinal));
    }

    public sealed record MutationCase(string Name, Action<CodeGenCoverageSamples> Apply);
}
