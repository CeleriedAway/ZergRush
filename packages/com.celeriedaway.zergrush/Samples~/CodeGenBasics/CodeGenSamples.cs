using System.Collections.Generic;
using UnityEngine;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Samples
{
    /*
     * To generate code press Shift + Alt + C in unity, or "Code Gen" > "Run CodeGen" from menu
     * Some time it is difficult to refactor code because of other generated code
     * And new code can be generated only if program is fully compiled, that is IMPORTANT!!!
     * Use "Code Gen" > "Generate Stubs" or Shift + Alt + S to generate stub code before or during your refactor
     * And when you program is compilable generate code normal way again
     *
     * Versioning is not supported for BinarySerialization by now
     * Json serialization is not very sensitive for versions 
     *
     * Code generation starts with defining tag with task enum value describing which functionality we want to generate
     * The simplest one is the following...
     */
    [GenTask(
        GenTaskFlags.Serialization |         // Fast binary serialize/deserialize methods
        GenTaskFlags.JsonSerialization |     // Json serialize/deserialize methods
        GenTaskFlags.Hash |                  // Fast hash code calculation
        GenTaskFlags.UpdateFrom |            // Deep copy optimized for copying into other created similar model 
        GenTaskFlags.CompareChech |          // Function that prints all differences between two models into error log 
        GenTaskFlags.DefaultConstructor |    // Generate Constructor that constructs all class type fields with defaults
        GenTaskFlags.PolymorphicConstruction // Allows to save ancestor as base class values as fields or in containers
    )]
    // All generated code will be placed into "x_generated" folder
    [GenInLocalFolder(1000)]
    public partial class CodeGenSamples : ISerializable
    {
        // All fields are automatically included
        int intField;
        // All properties are not included by default
        string stringPropWithoutTagNotIncluded { get; set; }
        // You need to specify which properties to include with GenInclude tag
        [GenInclude] string stringProp { get; set; }
        // You can ignore some fields with GenIgnoreTag
        [GenIgnore] int someTempIgnoredField;
        
        // all ref type fields considered not null by default, if null expect exception during generated function calls
        string stringFieldMustNotBeNull;
        
        // Use CanBeNull tag for fields that can be null so code for this case will be generated
        [CanBeNull] string stringFieldThatCanBeNull;
        
        // Extension methods for external classes used in generated classes will be generated.
        // But extension methods can't access private members, so be careful with that
        ExternalClass externalClass;
        Vector3 vector;

        // Other generated objects can be included
        [CanBeNull] OtherData otherData;
        
        // You can ignore specific parts of code generation, for example if you do not want default construction of this field
        [GenIgnore(GenTaskFlags.DefaultConstructor)]
        OtherData otherData2 = new OtherData();
        
        List<int> listsOfPrimitivesAreOk;
        List<OtherData> listsOfDataAreOk;
        int[] arraysAreOk;
        
        // Dictionaries are supported but not for deep copy (UpdateFrom) for now...
        [GenIgnore(GenTaskFlags.UpdateFrom)] Dictionary<int, OtherData> dictsAreOk;
        [GenIgnore(GenTaskFlags.UpdateFrom)] Dictionary<int, List<List<string>>> complexStructuresAreAlsoOk;

        // Nullable primitives and structs.
        int? nullablePrimitive;
        Vector3? nullableStruct;
        PlainStruct? nullablePlainStruct;
        TaggedStruct? nullableStructWithGenerationTags;
        SampleEnum enumValue;
        SampleEnum? nullableEnumValue;
        List<int?> listOfNullablePrimitives;
        [GenIgnore(GenTaskFlags.UpdateFrom)] Dictionary<string, int?> dictWithNullableValues;
        TestGeneric<int> genericWithPrimitive;
        TestGeneric<CustomStruct> genericWithCustomStruct;
        
        // NOT SUPPORTED
        [GenIgnore] int[,] multyDimArraysAreNotSupported;

        // ZergRush.Reactive primitives are supported
        Cell<OtherData> reactiveValue;
        [CanBeNull] Cell<OtherData> nullableReactiveValue;
        Cell<int?> reactiveNullablePrimitive;
        Cell<Vector3?> reactiveNullableStruct;
        Cell<TaggedStruct?> reactiveNullableStructWithGenerationTags;
        Cell<Cell<Cell<int?>>> nestedReactiveNullablePrimitive;
        ReactiveCollection<int> reactiveCollections;

        [GenIgnore(GenTaskFlags.DefaultConstructor)]
        public List<CodeGenSamples> ancestorArray = new List<CodeGenSamples>();

        public List<TestPolyGenericParent> genericAncestorArray;

        static void HowToUse()
        {
            var data = new CodeGenSamples();
            // json serialize
            string jsonData = data.WriteToJsonString();
            // binary serialize
            byte[] binaryData = data.WriteToByteArray();
            // json deserialize
            data = jsonData.ReadFromJson<CodeGenSamples>();
            // binary deserialize
            var data2 = binaryData.Read<CodeGenSamples>();
            
            // deep copy data2 into data
            data.UpdateFrom(data2);

            // compare data hashes
            if (data.CalculateHash() != data2.CalculateHash())
            {
                // and check for differences if hashes are not equal
                data.CompareCheck(data2);
            }
            
            // polymorphic construction example
            var ancestor = CreatePolymorphic((ushort)Types.Ancestor);
        }
    }

    // All class tags are inhereted, so its handy to create one base class for you model classes with all tags you want 
    [GenInLocalFolder]
    public partial class Ancestor : CodeGenSamples
    {
        public int fields;
    }

    [GenTask(GenTaskFlags.SimpleDataPack)]
    [GenInLocalFolder]
    public partial class OtherData
    {
        public int someData;
    }

    public struct PlainStruct
    {
        public int value;
        public Vector3 position;
    }

    public struct CustomStruct
    {
        public int id;
        [CanBeNull] public string name;
    }

    public class TestGeneric<T>
    {
        public T value;
        public List<T> values = new List<T>();
        public Cell<T> reactiveValue = new Cell<T>();
        public Dictionary<string, T> valuesByName = new Dictionary<string, T>();
    }

    [GenTask(GenTaskFlags.PolymorphicDataPack)]
    public partial class TestPolyGenericParent
    {
        public int intField;
    }

    [GenRegGenericInstance("int")]
    [GenRegGenericInstance("CodeGenSamples")]
    public partial class TestGenericAncestor<T> : TestPolyGenericParent
    {
        public T genericField;
        public string normalField;
    }

    public partial class TestGenericChild : TestGenericAncestor<int>
    {
        public int additionalField;
    }
    

    [GenTask(GenTaskFlags.SimpleDataPack)]
    [GenInLocalFolder]
    public partial struct TaggedStruct
    {
        public int id;
        [CanBeNull] public string label;
    }

    public enum SampleEnum
    {
        None,
        First,
        Second
    }

    [GenInLocalFolder]
    public class ExternalClass
    {
        public int somePublicField;
        // private fields are not included in extension methods generation
        int somePrivateField;
    }
}
