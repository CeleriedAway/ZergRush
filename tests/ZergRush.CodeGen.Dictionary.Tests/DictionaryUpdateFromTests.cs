namespace ZergRush.CodeGen.Dictionary.Tests;

public sealed class DictionaryUpdateFromTests
{
    [Fact]
    public void Reuses_destination_dictionary_and_matching_values()
    {
        var reusedValue = new DictionaryValue { number = -1 };
        var destination = new DictionaryUpdateFromSample
        {
            values =
            {
                [1] = reusedValue,
                [3] = new DictionaryValue { number = 300 }
            }
        };
        var source = new DictionaryUpdateFromSample
        {
            values =
            {
                [1] = new DictionaryValue { number = 100 },
                [2] = new DictionaryValue { number = 200 }
            }
        };
        var destinationDictionary = destination.values;

        destination.UpdateFrom(source, new ZRUpdateFromHelper());

        Assert.Same(destinationDictionary, destination.values);
        Assert.Same(reusedValue, destination.values[1]);
        Assert.Equal(100, destination.values[1].number);
    }

    [Fact]
    public void Deep_copies_new_values_and_removes_stale_keys()
    {
        var sourceValue = new DictionaryValue { number = 200 };
        var source = new DictionaryUpdateFromSample { values = { [2] = sourceValue } };
        var destination = new DictionaryUpdateFromSample
        {
            values = { [3] = new DictionaryValue { number = 300 } }
        };

        destination.UpdateFrom(source, new ZRUpdateFromHelper());

        Assert.NotSame(sourceValue, destination.values[2]);
        Assert.Equal(200, destination.values[2].number);
        Assert.False(destination.values.ContainsKey(3));
    }

    [Fact]
    public void Updating_from_itself_is_a_no_op()
    {
        var sample = new DictionaryUpdateFromSample
        {
            values = { [1] = new DictionaryValue { number = 100 } }
        };
        var value = sample.values[1];

        sample.UpdateFrom(sample, new ZRUpdateFromHelper());

        Assert.Same(value, sample.values[1]);
        Assert.Equal(100, sample.values[1].number);
    }

    [Fact]
    public void Matching_keys_and_reusable_values_update_without_allocations()
    {
        var source = new DictionaryUpdateFromSample
        {
            values = { [1] = new DictionaryValue { number = 100 } }
        };
        var destination = new DictionaryUpdateFromSample
        {
            values = { [1] = new DictionaryValue() }
        };
        var helper = new ZRUpdateFromHelper();

        destination.UpdateFrom(source, helper);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 1_000; ++i)
        {
            destination.UpdateFrom(source, helper);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal(0, allocated);
    }
}
