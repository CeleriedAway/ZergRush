using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Samples {

    public partial class CodeGenSamples : IUpdatableFrom<ZergRush.Samples.CodeGenSamples>, IHashable, ICompareChechable<ZergRush.Samples.CodeGenSamples>, IJsonSerializable, IPolymorphable
    {
        public enum Types : ushort
        {
            CodeGenSamples = 1,
            Ancestor = 2,
        }
        static Func<CodeGenSamples> [] polymorphConstructors = new Func<CodeGenSamples> [] {
            () => null, // 0
            () => new ZergRush.Samples.CodeGenSamples(), // 1
            () => new ZergRush.Samples.Ancestor(), // 2
        };
        public static CodeGenSamples CreatePolymorphic(System.UInt16 typeId) {
            return polymorphConstructors[typeId]();
        }
        public virtual void UpdateFrom(ZergRush.Samples.CodeGenSamples other) 
        {
            intField = other.intField;
            stringFieldMustNotBeNull = other.stringFieldMustNotBeNull;
            stringFieldThatCanBeNull = other.stringFieldThatCanBeNull;
            externalClass.UpdateFrom(other.externalClass);
            vector = other.vector;
            if (other.otherData == null) {
                otherData = null;
            }
            else { 
                if (otherData == null) {
                    otherData = new ZergRush.Samples.OtherData();
                }
                otherData.UpdateFrom(other.otherData);
            }
            otherData2.UpdateFrom(other.otherData2);
            listsOfPrimitivesAreOk.UpdateFrom(other.listsOfPrimitivesAreOk);
            listsOfDataAreOk.UpdateFrom(other.listsOfDataAreOk);
            var arraysAreOkCount = other.arraysAreOk.Length;
            var arraysAreOkTemp = arraysAreOk;
            Array.Resize(ref arraysAreOkTemp, arraysAreOkCount);
            arraysAreOk = arraysAreOkTemp;
            arraysAreOk.UpdateFrom(other.arraysAreOk);
            var __reactiveValue = reactiveValue.value;
            __reactiveValue.UpdateFrom(other.reactiveValue.value);
            reactiveValue.value = __reactiveValue;
            reactiveCollections.UpdateFrom(other.reactiveCollections);
            ancestorArray.UpdateFrom(other.ancestorArray);
            stringProp = other.stringProp;
        }
        public virtual void Deserialize(BinaryReader reader) 
        {
            intField = reader.ReadInt32();
            stringFieldMustNotBeNull = reader.ReadString();
            if (!reader.ReadBoolean()) {
                stringFieldThatCanBeNull = null;
            }
            else { 
                stringFieldThatCanBeNull = reader.ReadString();
            }
            externalClass.Deserialize(reader);
            vector = reader.ReadUnityEngine_Vector3();
            if (!reader.ReadBoolean()) {
                otherData = null;
            }
            else { 
                if (otherData == null) {
                    otherData = new ZergRush.Samples.OtherData();
                }
                otherData.Deserialize(reader);
            }
            otherData2.Deserialize(reader);
            listsOfPrimitivesAreOk.Deserialize(reader);
            listsOfDataAreOk.Deserialize(reader);
            arraysAreOk = reader.ReadSystem_Int32_Array();
            dictsAreOk.Deserialize(reader);
            complexStructuresAreAlsoOk.Deserialize(reader);
            reactiveValue.value.Deserialize(reader);
            reactiveCollections.Deserialize(reader);
            ancestorArray.Deserialize(reader);
            stringProp = reader.ReadString();
        }
        public virtual void Serialize(BinaryWriter writer) 
        {
            writer.Write(intField);
            writer.Write(stringFieldMustNotBeNull);
            if (stringFieldThatCanBeNull == null) writer.Write(false);
            else {
                writer.Write(true);
                writer.Write(stringFieldThatCanBeNull);
            }
            externalClass.Serialize(writer);
            vector.Serialize(writer);
            if (otherData == null) writer.Write(false);
            else {
                writer.Write(true);
                otherData.Serialize(writer);
            }
            otherData2.Serialize(writer);
            listsOfPrimitivesAreOk.Serialize(writer);
            listsOfDataAreOk.Serialize(writer);
            arraysAreOk.Serialize(writer);
            dictsAreOk.Serialize(writer);
            complexStructuresAreAlsoOk.Serialize(writer);
            reactiveValue.value.Serialize(writer);
            reactiveCollections.Serialize(writer);
            ancestorArray.Serialize(writer);
            writer.Write(stringProp);
        }
        public virtual ulong CalculateHash() 
        {
            System.UInt64 hash = 345093625;
            hash += (ulong)1833837802;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)intField;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (ulong)stringFieldMustNotBeNull.GetHashCode();
            hash += hash << 11; hash ^= hash >> 7;
            hash += stringFieldThatCanBeNull != null ? (ulong)stringFieldThatCanBeNull.GetHashCode() : 345093625;
            hash += hash << 11; hash ^= hash >> 7;
            hash += externalClass.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += vector.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += otherData != null ? otherData.CalculateHash() : 345093625;
            hash += hash << 11; hash ^= hash >> 7;
            hash += otherData2.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += listsOfPrimitivesAreOk.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += listsOfDataAreOk.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += arraysAreOk.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += dictsAreOk.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += complexStructuresAreAlsoOk.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += reactiveValue.value.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += reactiveCollections.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += ancestorArray.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += (ulong)stringProp.GetHashCode();
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public  CodeGenSamples() 
        {
            stringFieldMustNotBeNull = string.Empty;
            externalClass = new ZergRush.Samples.ExternalClass();
            listsOfPrimitivesAreOk = new System.Collections.Generic.List<int>();
            listsOfDataAreOk = new System.Collections.Generic.List<ZergRush.Samples.OtherData>();
            arraysAreOk = Array.Empty<System.Int32>();
            dictsAreOk = new System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData>();
            complexStructuresAreAlsoOk = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>>();
            reactiveValue = new ZergRush.ReactiveCore.Cell<ZergRush.Samples.OtherData>();
            reactiveCollections = new ZergRush.ReactiveCore.ReactiveCollection<int>();
            stringProp = string.Empty;
        }
        public virtual void CompareCheck(ZergRush.Samples.CodeGenSamples other, Stack<string> __path) 
        {
            if (intField != other.intField) SerializationTools.LogCompError(__path, "intField", other.intField, intField);
            if (stringFieldMustNotBeNull != other.stringFieldMustNotBeNull) SerializationTools.LogCompError(__path, "stringFieldMustNotBeNull", other.stringFieldMustNotBeNull, stringFieldMustNotBeNull);
            if (stringFieldThatCanBeNull != other.stringFieldThatCanBeNull) SerializationTools.LogCompError(__path, "stringFieldThatCanBeNull", other.stringFieldThatCanBeNull, stringFieldThatCanBeNull);
            __path.Push("externalClass");
            externalClass.CompareCheck(other.externalClass, __path);
            __path.Pop();
            __path.Push("vector");
            vector.CompareCheck(other.vector, __path);
            __path.Pop();
            if (SerializationTools.CompareNull(__path, "otherData", otherData, other.otherData)) {
                __path.Push("otherData");
                otherData.CompareCheck(other.otherData, __path);
                __path.Pop();
            }
            __path.Push("otherData2");
            otherData2.CompareCheck(other.otherData2, __path);
            __path.Pop();
            __path.Push("listsOfPrimitivesAreOk");
            listsOfPrimitivesAreOk.CompareCheck(other.listsOfPrimitivesAreOk, __path);
            __path.Pop();
            __path.Push("listsOfDataAreOk");
            listsOfDataAreOk.CompareCheck(other.listsOfDataAreOk, __path);
            __path.Pop();
            __path.Push("arraysAreOk");
            arraysAreOk.CompareCheck(other.arraysAreOk, __path);
            __path.Pop();
            __path.Push("dictsAreOk");
            dictsAreOk.CompareCheck(other.dictsAreOk, __path);
            __path.Pop();
            __path.Push("complexStructuresAreAlsoOk");
            complexStructuresAreAlsoOk.CompareCheck(other.complexStructuresAreAlsoOk, __path);
            __path.Pop();
            __path.Push("reactiveValue");
            reactiveValue.value.CompareCheck(other.reactiveValue.value, __path);
            __path.Pop();
            __path.Push("reactiveCollections");
            reactiveCollections.CompareCheck(other.reactiveCollections, __path);
            __path.Pop();
            __path.Push("ancestorArray");
            ancestorArray.CompareCheck(other.ancestorArray, __path);
            __path.Pop();
            if (stringProp != other.stringProp) SerializationTools.LogCompError(__path, "stringProp", other.stringProp, stringProp);
        }
        public virtual void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "intField":
                intField = (int)(Int64)reader.Value;
                break;
                case "stringFieldMustNotBeNull":
                stringFieldMustNotBeNull = (string) reader.Value;
                break;
                case "stringFieldThatCanBeNull":
                if (reader.TokenType == JsonToken.Null) {
                    stringFieldThatCanBeNull = null;
                }
                else { 
                    stringFieldThatCanBeNull = (string) reader.Value;
                }
                break;
                case "externalClass":
                externalClass.ReadFromJson(reader);
                break;
                case "vector":
                vector = (UnityEngine.Vector3)reader.ReadFromJsonUnityEngine_Vector3();
                break;
                case "otherData":
                if (reader.TokenType == JsonToken.Null) {
                    otherData = null;
                }
                else { 
                    if (otherData == null) {
                        otherData = new ZergRush.Samples.OtherData();
                    }
                    otherData.ReadFromJson(reader);
                }
                break;
                case "otherData2":
                otherData2.ReadFromJson(reader);
                break;
                case "listsOfPrimitivesAreOk":
                listsOfPrimitivesAreOk.ReadFromJson(reader);
                break;
                case "listsOfDataAreOk":
                listsOfDataAreOk.ReadFromJson(reader);
                break;
                case "arraysAreOk":
                arraysAreOk = arraysAreOk.ReadFromJson(reader);
                break;
                case "dictsAreOk":
                dictsAreOk.ReadFromJson(reader);
                break;
                case "complexStructuresAreAlsoOk":
                complexStructuresAreAlsoOk.ReadFromJson(reader);
                break;
                case "reactiveValue":
                reactiveValue.value.ReadFromJson(reader);
                break;
                case "reactiveCollections":
                reactiveCollections.ReadFromJson(reader);
                break;
                case "ancestorArray":
                ancestorArray.ReadFromJson(reader);
                break;
                case "stringProp":
                stringProp = (string) reader.Value;
                break;
            }
        }
        public virtual void WriteJsonFields(JsonTextWriter writer) 
        {
            writer.WritePropertyName("intField");
            writer.WriteValue(intField);
            writer.WritePropertyName("stringFieldMustNotBeNull");
            writer.WriteValue(stringFieldMustNotBeNull);
            if (stringFieldThatCanBeNull == null)
            {
                writer.WritePropertyName("stringFieldThatCanBeNull");
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName("stringFieldThatCanBeNull");
                writer.WriteValue(stringFieldThatCanBeNull);
            }
            writer.WritePropertyName("externalClass");
            externalClass.WriteJson(writer);
            writer.WritePropertyName("vector");
            vector.WriteJson(writer);
            if (otherData == null)
            {
                writer.WritePropertyName("otherData");
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName("otherData");
                otherData.WriteJson(writer);
            }
            writer.WritePropertyName("otherData2");
            otherData2.WriteJson(writer);
            writer.WritePropertyName("listsOfPrimitivesAreOk");
            listsOfPrimitivesAreOk.WriteJson(writer);
            writer.WritePropertyName("listsOfDataAreOk");
            listsOfDataAreOk.WriteJson(writer);
            writer.WritePropertyName("arraysAreOk");
            arraysAreOk.WriteJson(writer);
            writer.WritePropertyName("dictsAreOk");
            dictsAreOk.WriteJson(writer);
            writer.WritePropertyName("complexStructuresAreAlsoOk");
            complexStructuresAreAlsoOk.WriteJson(writer);
            writer.WritePropertyName("reactiveValue");
            reactiveValue.value.WriteJson(writer);
            writer.WritePropertyName("reactiveCollections");
            reactiveCollections.WriteJson(writer);
            writer.WritePropertyName("ancestorArray");
            ancestorArray.WriteJson(writer);
            writer.WritePropertyName("stringProp");
            writer.WriteValue(stringProp);
        }
        public virtual ushort GetClassId() 
        {
        return (System.UInt16)Types.CodeGenSamples;
        }
        public virtual ZergRush.Samples.CodeGenSamples NewInst() 
        {
        return new CodeGenSamples();
        }
    }
}
#endif
