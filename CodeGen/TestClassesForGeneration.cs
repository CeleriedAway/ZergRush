using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using ZergRush.Alive;
using TestGen;
using ZergRush.ReactiveCore;

namespace TestGen
{
//    public enum TestEnum : ushort
//    {
//        Value1,
//        Value2,
//        Value3,
//    }
//
//    [GenTask(GenTaskFlags.BattleData | GenTaskFlags.Serialize)]
//    public partial class TestPolymorphParent : IPolymorphable
//    {
//        public int val1;
//    }
//
//    [GenTask(GenTaskFlags.BattleData | GenTaskFlags.Serialize)]
//    public partial class TestPolymorphChild : TestPolymorphParent
//    {
//        public int val2;
//    }
//
//    [GenTask(GenTaskFlags.BattleData | GenTaskFlags.Serialize)]
//    public partial class TestPolymorphChild2 : TestPolymorphParent
//    {
//        public int val3;
//    }
//
//    [GenTask(GenTaskFlags.Serialize | GenTaskFlags.UpdateFrom)]
//    [GenTask(GenTaskFlags.Hash)]
//    [GenHashing]
//    public abstract partial class GenericSerializationExample<T>
//        where T : ISerializable, IUpdatableFrom<T>, IHashable, new()
//    {
//        public PrimitiveFieldTest test;
//
//        [GenIgnore(GenTaskFlags.Hash)] public Cell<T> field;
//
//        [GenInclude(GenTaskFlags.UpdateFrom | GenTaskFlags.Serialize)]
//        public int propField { get; set; }
//
//        public T field2;
//        public List<T> listFIeld;
//        public int intField;
//        public List<TestPolymorphParent> poolymorphList;
//        [CanBeNull] public TestPolymorphParent polymorphInstance;
//
//        static Func<TestPolymorphParent>[] TestPolymorphParentCunstructorTable = new Func<TestPolymorphParent>[]
//        {
//            () => new TestPolymorphParent(),
//            () => new TestPolymorphChild(),
//            () => new TestPolymorphChild2(),
//        };
//
//        public static TestPolymorphParent CreateTestPolymorphParent(UInt16 classId)
//        {
//            return TestPolymorphParentCunstructorTable[classId]();
//        }
//    }
//
//
//    [GenTask(GenTaskFlags.BattleData)]
//    public abstract partial class InheritedGenericType<T> : GenericSerializationExample<T>
//        where T : IHashable, ISerializable, IUpdatableFrom<T>, new()
//    {
//        public float bla;
//        public float bla2;
//    }
//
//    [GenHashing]
//    public partial class YetAnotherGenericTest : IUpdatableFrom<YetAnotherGenericTest>, ISerializable
//    {
//        public int field;
//
//        public void UpdateFrom(YetAnotherGenericTest val)
//        {
//            throw new NotImplementedException();
//        }
//
//        public void Serialize(BinaryWriter writer)
//        {
//            throw new NotImplementedException();
//        }
//
//        public void Deserialize(BinaryReader reader)
//        {
//            throw new NotImplementedException();
//        }
//    }
//
//
//    [GenTask(GenTaskFlags.Hash | GenTaskFlags.Serialize | GenTaskFlags.UpdateFrom)]
//    public partial class PrimitiveFieldTest : ISerializable, IHashable
//    {
//        public int val;
//        public int intField;
//        public bool boolField;
//        public uint uintField;
//        public short shortField;
//        public ushort ushortField;
//        public byte byteField;
//        public ulong ulongField;
//        public long longField;
//        public string strVal;
//
//        public TestEnum enumField;
//        public Vector2 vecField;
//        public Matrix4x4 matrixField;
//        public byte[] byteArray;
//
//        [GenInclude] public Vector2 vecFieldProp { get; set; }
//
//        [GenIgnore] public int ignoredField;
//    }

//    [GenSerialization, GenUpdateFrom]
//    public partial class CustomSerializationTest : ICustomSerialization, ICustomUpdateFrom<CustomSerializationTest>
//    {
//        int f1;
//        string f2;
//        PrimitiveFieldTest f3;
//        
//        public void Serialize(BinaryWriter writer)
//        {
//            throw new NotImplementedException();
//        }
//
//        public void Deserialize(BinaryReader reader)
//        {
//            throw new NotImplementedException();
//        }
//
//        public void UpdateFrom(CustomSerializationTest val)
//        {
//            throw new NotImplementedException();
//        }
//    }
//
//    [GenSerialization, GenUpdateFrom, GenHashing]
//    public partial class ParentClassTest
//    {
//        public byte[] dataArray;
//        [CanBeNull]
//        public byte[] dataArrayNullable;
//        [CanBeNull]
//        public byte[] stringNullable;
//        
//    }
//
//    [GenUpdateFrom]
//    public partial class UpdatableOnlyTest
//    {
//        
//        public byte[] dataArray;
//        public int val;
//        public string strVal;
//        public TestEnum enumField;
//        public Vector2 vecField;
//        public Matrix4x4 matrixField;
//    }
//    
//    [GenTask(GenTaskFlags.BattleData | GenTaskFlags.Serialize)]
//    public partial class ComplexFieldTest : ISerializable
//    {
//        [CanBeNull] public PrimitiveFieldTest innerFieldNullable;
//        [CanBeNull] public PrimitiveFieldTest innerFieldNullable2;
//        public PrimitiveFieldTest innerField = new PrimitiveFieldTest();
//
//        public List<PrimitiveFieldTest> listField = new List<PrimitiveFieldTest>();
//        [CanBeNull] public List<PrimitiveFieldTest> listNullable = new List<PrimitiveFieldTest>();
//        public Cell<int> intCell = new Cell<int>();
//        public Cell<PrimitiveFieldTest> complexCell = new Cell<PrimitiveFieldTest>(new PrimitiveFieldTest());
//
//        // Cell's content can be null
//        public Cell<string> complexCellNullableContent = new Cell<string>();
//
//        public List<short> shortList = new List<short>();
//
//        [CanBeNull] public TestPolymorphParent polymorphField = new TestPolymorphChild2();
//        public List<TestPolymorphParent> polymorphList = new List<TestPolymorphParent>();
//
//        public PrimitiveFieldTest[] simpleArray;
//        public TestPolymorphParent[] polymorphArray;
//    }

}

    namespace ZergRush.Alive
    {

    }
