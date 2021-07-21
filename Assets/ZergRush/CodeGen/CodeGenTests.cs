using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZergRush.Alive;
using UnityEngine;
using ZergRush;
using ZergRush.ReactiveCore;
using Random = System.Random;

namespace TestGen
{
    public static  class CodeGenTests
    {
//        static Random rnd = new Random();
//        static long RandomLong()
//        {
//            return rnd.Next() << 32 | rnd.Next();
//        }
//
//        static PrimitiveFieldTest PrimFields()
//        {
//            var inst = new PrimitiveFieldTest();
//            inst.boolField = true;
//            inst.ignoredField = (int) RandomLong();
//            inst.intField = (int) RandomLong();
//            inst.longField = RandomLong();
//            inst.byteField = (byte) RandomLong();
//            inst.shortField = (short) RandomLong();
//
//            inst.matrixField.m03 = 10;
//            inst.matrixField.m00 = RandomLong();
//            inst.matrixField.m22 = RandomLong();
//            inst.matrixField.m32 = RandomLong();
//            
//            inst.vecField = new Vector2(RandomLong(), RandomLong());
//
//            inst.strVal = "test_str";
//            inst.byteArray = new byte[] {32, 32, 55, 53, 34, 43};
//
//            return inst;
//        }
//
//        public static bool CellsAreEqual(Cell<PrimitiveFieldTest> c1, Cell<PrimitiveFieldTest> c2)
//        {
//            if (c1 == null && c2 == null) return true;
//            if (c1 != null && c2 == null) return false;
//            if (c1 == null && c2 != null) return false;
//            return PEquals(c1.value, c2.value);
//        }
//        public static bool CellsAreEqual<T>(Cell<T> c1, Cell<T> c2) where T : IEquatable<T>
//        {
//            if (c1 == null && c2 == null) return true;
//            if (c1 != null && c2 == null) return false;
//            if (c1 == null && c2 != null) return false;
//            return object.Equals(c1.value, c2.value);
//        }
//        public static bool ListAreEquals(List<PrimitiveFieldTest> l1, List<PrimitiveFieldTest> l2)
//        {
//            if (l1 == null && l2 == null) return true;
//            if (l1 != null && l2 == null) return false;
//            if (l1 == null && l2 != null) return false;
//            if (l1.Count != l2.Count) return false;
//            for (int i = 0; i < l1.Count; i++)
//            {
//                if (PEquals(l1[i], l2[i]) == false) return false;
//            }
//            return true;
//        }
//        public static bool ArraysAreEquals(PrimitiveFieldTest [] l1, PrimitiveFieldTest [] l2)
//        {
//            if (l1 == null && l2 == null) return true;
//            if (l1 != null && l2 == null) return false;
//            if (l1 == null && l2 != null) return false;
//            if (l1.Length != l2.Length) return false;
//            for (int i = 0; i < l1.Length; i++)
//            {
//                if (PEquals(l1[i], l2[i]) == false) return false;
//            }
//            return true;
//        }
//        public static bool ListAreEquals(List<short> l1, List<short> l2)
//        {
//            if (l1 == null && l2 == null) return true;
//            if (l1 != null && l2 == null) return false;
//            if (l1 == null && l2 != null) return false;
//            if (l1.Count != l2.Count) return false;
//            for (int i = 0; i < l1.Count; i++)
//            {
//                if (l1[i] != l2[i]) return false;
//            }
//            return true;
//        }
//
//        public static List<PrimitiveFieldTest> RandomList()
//        {
//            var list = new List<PrimitiveFieldTest>();
//            var count = rnd.Next() % 10;
//            for (int i = 0; i < count; i++)
//            {
//                list.Add(PrimFields());
//            }
//            return list;
//        }
//        public static List<short> RandomShortList()
//        {
//            var list = new List<short>();
//            var count = rnd.Next() % 10;
//            for (int i = 0; i < count; i++)
//            {
//                list.Add((short)RandomLong());
//            }
//            return list;
//        }
//
//        static bool ComparePolymorphList(List<TestPolymorphParent> l1, List<TestPolymorphParent> l2)
//        {
//            if (l1 == null && l2 == null) return true;
//            if (l1 != null && l2 == null) return false;
//            if (l1 == null && l2 != null) return false;
//            if (l1.Count != l2.Count) return false;
//            for (int i = 0; i < l1.Count; i++)
//            {
//                if (ComparePolymorph(l1[i], l2[i]) == false) return false;
//            }
//            return true;
//        }
//        
//        static bool ComparePolymorphArray(TestPolymorphParent [] l1, TestPolymorphParent [] l2)
//        {
//            if (l1 == null && l2 == null) return true;
//            if (l1 != null && l2 == null) return false;
//            if (l1 == null && l2 != null) return false;
//            if (l1.Length != l2.Length) return false;
//            for (int i = 0; i < l1.Length; i++)
//            {
//                if (ComparePolymorph(l1[i], l2[i]) == false) return false;
//            }
//            return true;
//        }
//
//        static bool ComparePolymorph(TestPolymorphParent inst1, TestPolymorphParent inst2)
//        {
//            if (inst1.GetType() != inst2.GetType()) return false;
//            if (inst1.GetType() == typeof(TestPolymorphChild))
//            {
//                var i1 = (TestPolymorphChild) inst1;
//                var i2 = (TestPolymorphChild) inst2;
//                return i1.val1 == i2.val1 && i1.val2 == i2.val2;
//            }
//            if (inst1.GetType() == typeof(TestPolymorphChild2))
//            {
//                var i1 = (TestPolymorphChild2) inst1;
//                var i2 = (TestPolymorphChild2) inst2;
//                return i1.val1 == i2.val1 && i1.val3 == i2.val3;
//            }
//            if (inst1.GetType() == typeof(TestPolymorphParent))
//            {
//                var i1 = (TestPolymorphParent) inst1;
//                var i2 = (TestPolymorphParent) inst2;
//                return i1.val1 == i2.val1;
//            }
//            return false;
//        }
//
//        static TestPolymorphParent RandPInst()
//        {
//            return new List<TestPolymorphParent>
//            {
//                new TestPolymorphParent{val1 = (int)RandomLong()},
//                new TestPolymorphChild(){val1 = (int)RandomLong(), val2 = (int)RandomLong()},
//                new TestPolymorphChild2(){val1 = (int)RandomLong(), val3 = (int)RandomLong()},
//            }.RandomElement();
//        }
//
//        static List<TestPolymorphParent> RandPList()
//        {
//            var list = new List<TestPolymorphParent>();
//            var count = rnd.Next() % 10;
//            for (int i = 0; i < count; i++)
//            {
//                list.Add(RandPInst());
//            }
//            return list;
//        }
//        
//        static TestPolymorphParent [] RandPArray()
//        {
//            var count = rnd.Next() % 10;
//            var list = new TestPolymorphParent[count];
//            for (int i = 0; i < count; i++)
//            {
//                list[i] = RandPInst();
//            }
//            return list;
//        }
//        
//        static PrimitiveFieldTest [] RandArray()
//        {
//            var count = rnd.Next() % 10;
//            var list = new PrimitiveFieldTest[count];
//            for (int i = 0; i < count; i++)
//            {
//                list[i] = PrimFields();
//            }
//            return list;
//        }
//        
//
//        static ComplexFieldTest ComplexFields()
//        {
//            var inst = new ComplexFieldTest();
//            inst.complexCell.value = PrimFields();
//            inst.complexCellNullableContent.value = "test cSell";
//            inst.innerField = PrimFields();
//            inst.innerFieldNullable = null;
//            inst.innerFieldNullable2 = PrimFields();
//            inst.intCell.value = (int)RandomLong();
//            inst.listField = RandomList();
//            inst.listNullable = null;
//            inst.shortList = RandomShortList();
//            var child = new TestPolymorphChild();
//            child.val2 = 1234;
//            inst.polymorphField = RandPInst();
//            inst.polymorphList = RandPList();
//            inst.simpleArray = RandArray();
//            inst.polymorphArray = RandPArray();
//            return inst;
//        }
//
//        static bool CEqual(ComplexFieldTest c1, ComplexFieldTest c2)
//        {
//            return
//                CellsAreEqual(c1.complexCell, c2.complexCell) &&
//                CellsAreEqual(c1.complexCellNullableContent, c2.complexCellNullableContent) &&
//                PEquals(c1.innerField, c2.innerField) &&
//                PEquals(c1.innerFieldNullable, c2.innerFieldNullable) &&
//                PEquals(c1.innerFieldNullable2, c2.innerFieldNullable2) &&
//                ListAreEquals(c1.listField, c1.listField) &&
//                ListAreEquals(c1.listNullable, c1.listNullable) &&
//                ListAreEquals(c1.shortList, c1.shortList) && 
//                ComparePolymorph(c1.polymorphField, c2.polymorphField) &&
//                ComparePolymorphList(c1.polymorphList, c2.polymorphList) && 
//                ArraysAreEquals(c1.simpleArray, c2.simpleArray) &&
//                ComparePolymorphArray(c1.polymorphArray, c2.polymorphArray);
//        }
//
//        static bool PEquals(PrimitiveFieldTest p1, PrimitiveFieldTest p2)
//        {
//            if (p1 == null && p2 == null) return true;
//            if (p1 != null ^ p2 != null) return false;
//            return
//                p1.boolField == p2.boolField &&
//                p1.byteField == p2.byteField &&
//                p1.enumField == p2.enumField &&
//                p1.intField == p2.intField &&
//                p1.longField == p2.longField &&
//                p1.matrixField == p2.matrixField &&
//                p1.vecField == p2.vecField &&
//                p1.byteArray.SequenceEqual(p2.byteArray) &&
//                p1.strVal == p2.strVal &&
//                p1.uintField == p2.uintField &&
//                p1.ushortField == p2.ushortField &&
//                p1.vecFieldProp == p2.vecFieldProp&&
//                p1.byteField == p2.byteField 
//                ;
//        }
//
//        public static byte[] Serialize<T>(this T inst) where T : ISerializable
//        {
//			using (var ms = new MemoryStream())
//			{
//				inst.Serialize(new BinaryWriter(ms));
//				return ms.ToArray();
//			}
//        }
//        public static T Deserialize<T>(byte [] data) where T : ISerializable, new()
//        {
//            var t = new T();
//			using (var ms = new MemoryStream(data))
//			{
//				t.Deserialize(new BinaryReader(ms));
//			}
//            return t;
//        }
//        
//        public static void PrimitiveSerializationTest()
//        {
//            var prim = PrimFields();
//            var data = prim.Serialize();
//            var deserialized = Deserialize<PrimitiveFieldTest>(data);
//            Debug.Assert(PEquals(prim, deserialized));
//
//            var updated = PrimFields();
//            updated.UpdateFrom(deserialized);
//            Debug.Assert(PEquals(updated, prim));
//            
//            Debug.Log("Hash of initial class = " + prim.CalculateHash());
//            Debug.Log("Hash of deserialized = " + deserialized.CalculateHash());
//            Debug.Log("Hash of updated = " + updated.CalculateHash());
//        }
//        
//        public static void ComplexSerializationTest()
//        {
//            var inst = ComplexFields();
//            var data = inst.Serialize();
//            var deserialized = Deserialize<ComplexFieldTest>(data);
//            Debug.Assert(CEqual(inst, deserialized));
//
//            var updated = ComplexFields();
//            updated.UpdateFrom(deserialized);
//            Debug.Assert(CEqual(updated, inst));
//            
//            Debug.Log("Hash of initial class = " + inst.CalculateHash());
//            Debug.Log("Hash of deserialized = " + deserialized.CalculateHash());
//            Debug.Log("Hash of updated = " + updated.CalculateHash());
//
//            inst.listField.RandomElement().enumField = TestEnum.Value3;
//            Debug.Log("Hash of changed class = " + inst.CalculateHash());
//        }
    }
}