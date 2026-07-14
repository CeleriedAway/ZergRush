using System.Collections.Generic;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;
using ZergRush.Samples;

namespace ZergRush.CodeGen.Tests;

[GenTask(
    GenTaskFlags.Serialization |
    GenTaskFlags.JsonSerialization |
    GenTaskFlags.Hash |
    GenTaskFlags.UpdateFrom |
    GenTaskFlags.CompareChech |
    GenTaskFlags.DefaultConstructor)]
[GenInLocalFolder]
public partial class CodeGenCoverageSamples
{
    public bool booleanValue;
    public byte byteValue;
    public short shortValue;
    public int intValue;
    public long longValue;
    public float floatValue;
    public double doubleValue;
    public decimal decimalValue;
    public char charValue;
    public string requiredString = string.Empty;
    [CanBeNull] public string nullableString;
    public SampleEnum enumValue;
    public SampleEnum? nullableEnumValue;

    public int? nullableIntValue;
    public PlainStruct plainStruct;
    public PlainStruct? nullablePlainStruct;
    public TaggedStruct taggedStruct;
    public TaggedStruct? nullableTaggedStruct;

    public OtherData requiredObject = new();
    [CanBeNull] public OtherData nullableObject;
    public List<int> primitiveList = new();
    public List<int?> nullablePrimitiveList = new();
    public List<OtherData> objectList = new();
    public List<List<string>> nestedList = new();
    public int[] primitiveArray = Array.Empty<int>();

    public Dictionary<string, int> primitiveDictionary = new();
    public Dictionary<string, int?> nullablePrimitiveDictionary = new();
    public Dictionary<string, OtherData> objectDictionary = new();
    public Dictionary<int, List<List<string>>> nestedDictionary = new();

    public Cell<int?> nullableCell = new();
    public Cell<OtherData> objectCell = new();
    public Cell<Cell<Cell<int?>>> nestedNullableCell = new();
    public ReactiveCollection<int> reactiveCollection = new();
}
