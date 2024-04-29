using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Samples {

    public partial class CodeGenSamples : IUpdatableFrom<ZergRush.Samples.CodeGenSamples>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareChechable<ZergRush.Samples.CodeGenSamples>, IJsonSerializable, IPolymorphable, ICloneInst
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
        public virtual void UpdateFrom(ZergRush.Samples.CodeGenSamples other, ZRUpdateFromHelper __helper) 
        {
            ancestorArray.UpdateFrom(other.ancestorArray, __helper);
            var arraysAreOkCount = other.arraysAreOk.Length;
            var arraysAreOkTemp = arraysAreOk;
            Array.Resize(ref arraysAreOkTemp, arraysAreOkCount);
            arraysAreOk = arraysAreOkTemp;
            arraysAreOk.UpdateFrom(other.arraysAreOk, __helper);
            externalClass.UpdateFrom(other.externalClass, __helper);
            intField = other.intField;
            listsOfDataAreOk.UpdateFrom(other.listsOfDataAreOk, __helper);
            listsOfPrimitivesAreOk.UpdateFrom(other.listsOfPrimitivesAreOk, __helper);
            if (other.otherData == null) {
                otherData = null;
            }
            else { 
                if (otherData == null) {
                    otherData = new ZergRush.Samples.OtherData();
                }
                otherData.UpdateFrom(other.otherData, __helper);
            }
            otherData2.UpdateFrom(other.otherData2, __helper);
            reactiveCollections.UpdateFrom(other.reactiveCollections, __helper);
            var __reactiveValue = reactiveValue.value;
            __reactiveValue.UpdateFrom(other.reactiveValue.value, __helper);
            reactiveValue.value = __reactiveValue;
            stringFieldMustNotBeNull = other.stringFieldMustNotBeNull;
            stringFieldThatCanBeNull = other.stringFieldThatCanBeNull;
            stringProp = other.stringProp;
            vector.UpdateFrom(other.vector, __helper);
        }
        public virtual void Deserialize(ZRBinaryReader reader) 
        {
            ancestorArray.Deserialize(reader);
            arraysAreOk = reader.ReadSystem_Int32_Array();
            complexStructuresAreAlsoOk.Deserialize(reader);
            dictsAreOk.Deserialize(reader);
            externalClass.Deserialize(reader);
            intField = reader.ReadInt32();
            listsOfDataAreOk.Deserialize(reader);
            listsOfPrimitivesAreOk.Deserialize(reader);
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
            reactiveCollections.Deserialize(reader);
            reactiveValue.value.Deserialize(reader);
            stringFieldMustNotBeNull = reader.ReadString();
            if (!reader.ReadBoolean()) {
                stringFieldThatCanBeNull = null;
            }
            else { 
                stringFieldThatCanBeNull = reader.ReadString();
            }
            stringProp = reader.ReadString();
            vector = reader.ReadUnityEngine_Vector3();
        }
        public virtual void Serialize(ZRBinaryWriter writer) 
        {
            ancestorArray.Serialize(writer);
            arraysAreOk.Serialize(writer);
            complexStructuresAreAlsoOk.Serialize(writer);
            dictsAreOk.Serialize(writer);
            externalClass.Serialize(writer);
            writer.Write(intField);
            listsOfDataAreOk.Serialize(writer);
            listsOfPrimitivesAreOk.Serialize(writer);
            if (otherData == null) writer.Write(false);
            else {
                writer.Write(true);
                otherData.Serialize(writer);
            }
            otherData2.Serialize(writer);
            reactiveCollections.Serialize(writer);
            reactiveValue.value.Serialize(writer);
            writer.Write(stringFieldMustNotBeNull);
            if (stringFieldThatCanBeNull == null) writer.Write(false);
            else {
                writer.Write(true);
                writer.Write(stringFieldThatCanBeNull);
            }
            writer.Write(stringProp);
            vector.Serialize(writer);
        }
        public virtual ulong CalculateHash(ZRHashHelper __helper) 
        {
            System.UInt64 hash = 345093625;
            hash ^= (ulong)2069206495;
            hash += hash << 11; hash ^= hash >> 7;
            hash += ancestorArray.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += arraysAreOk.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += complexStructuresAreAlsoOk.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += dictsAreOk.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += externalClass.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)intField;
            hash += hash << 11; hash ^= hash >> 7;
            hash += listsOfDataAreOk.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += listsOfPrimitivesAreOk.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += otherData != null ? otherData.CalculateHash(__helper) : 345093625;
            hash += hash << 11; hash ^= hash >> 7;
            hash += otherData2.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += reactiveCollections.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += reactiveValue.value.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            hash += (ulong)stringFieldMustNotBeNull.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += stringFieldThatCanBeNull != null ? (ulong)stringFieldThatCanBeNull.CalculateHash() : 345093625;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (ulong)stringProp.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += vector.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public  CodeGenSamples() 
        {
            arraysAreOk = Array.Empty<System.Int32>();
            complexStructuresAreAlsoOk = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>>();
            dictsAreOk = new System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData>();
            externalClass = new ZergRush.Samples.ExternalClass();
            listsOfDataAreOk = new System.Collections.Generic.List<ZergRush.Samples.OtherData>();
            listsOfPrimitivesAreOk = new System.Collections.Generic.List<int>();
            reactiveCollections = new ZergRush.ReactiveCore.ReactiveCollection<int>();
            reactiveValue = new ZergRush.ReactiveCore.Cell<ZergRush.Samples.OtherData>();
            stringFieldMustNotBeNull = string.Empty;
            stringProp = string.Empty;
        }
        public virtual void CompareCheck(ZergRush.Samples.CodeGenSamples other, ZRCompareCheckHelper __helper, Action<string> printer) 
        {
            __helper.Push("ancestorArray");
            ancestorArray.CompareCheck(other.ancestorArray, __helper, printer);
            __helper.Pop();
            __helper.Push("arraysAreOk");
            arraysAreOk.CompareCheck(other.arraysAreOk, __helper, printer);
            __helper.Pop();
            __helper.Push("complexStructuresAreAlsoOk");
            complexStructuresAreAlsoOk.CompareCheck(other.complexStructuresAreAlsoOk, __helper, printer);
            __helper.Pop();
            __helper.Push("dictsAreOk");
            dictsAreOk.CompareCheck(other.dictsAreOk, __helper, printer);
            __helper.Pop();
            __helper.Push("externalClass");
            externalClass.CompareCheck(other.externalClass, __helper, printer);
            __helper.Pop();
            if (intField != other.intField) SerializationTools.LogCompError(__helper, "intField", printer, other.intField, intField);
            __helper.Push("listsOfDataAreOk");
            listsOfDataAreOk.CompareCheck(other.listsOfDataAreOk, __helper, printer);
            __helper.Pop();
            __helper.Push("listsOfPrimitivesAreOk");
            listsOfPrimitivesAreOk.CompareCheck(other.listsOfPrimitivesAreOk, __helper, printer);
            __helper.Pop();
            if (SerializationTools.CompareNull(__helper, "otherData", printer, otherData, other.otherData)) {
                __helper.Push("otherData");
                otherData.CompareCheck(other.otherData, __helper, printer);
                __helper.Pop();
            }
            __helper.Push("otherData2");
            otherData2.CompareCheck(other.otherData2, __helper, printer);
            __helper.Pop();
            __helper.Push("reactiveCollections");
            reactiveCollections.CompareCheck(other.reactiveCollections, __helper, printer);
            __helper.Pop();
            __helper.Push("reactiveValue");
            reactiveValue.value.CompareCheck(other.reactiveValue.value, __helper, printer);
            __helper.Pop();
            if (stringFieldMustNotBeNull != other.stringFieldMustNotBeNull) SerializationTools.LogCompError(__helper, "stringFieldMustNotBeNull", printer, other.stringFieldMustNotBeNull, stringFieldMustNotBeNull);
            if (stringFieldThatCanBeNull != other.stringFieldThatCanBeNull) SerializationTools.LogCompError(__helper, "stringFieldThatCanBeNull", printer, other.stringFieldThatCanBeNull, stringFieldThatCanBeNull);
            if (stringProp != other.stringProp) SerializationTools.LogCompError(__helper, "stringProp", printer, other.stringProp, stringProp);
            __helper.Push("vector");
            vector.CompareCheck(other.vector, __helper, printer);
            __helper.Pop();
        }
        public virtual bool ReadFromJsonField(ZRJsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "ancestorArray":
                ancestorArray.ReadFromJson(reader);
                break;
                case "arraysAreOk":
                arraysAreOk = arraysAreOk.ReadFromJson(reader);
                break;
                case "complexStructuresAreAlsoOk":
                complexStructuresAreAlsoOk.ReadFromJson(reader);
                break;
                case "dictsAreOk":
                dictsAreOk.ReadFromJson(reader);
                break;
                case "externalClass":
                externalClass.ReadFromJson(reader);
                break;
                case "intField":
                intField = (int)(Int64)reader.Value;
                break;
                case "listsOfDataAreOk":
                listsOfDataAreOk.ReadFromJson(reader);
                break;
                case "listsOfPrimitivesAreOk":
                listsOfPrimitivesAreOk.ReadFromJson(reader);
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
                case "reactiveCollections":
                reactiveCollections.ReadFromJson(reader);
                break;
                case "reactiveValue":
                reactiveValue.value.ReadFromJson(reader);
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
                case "stringProp":
                stringProp = (string) reader.Value;
                break;
                case "vector":
                vector = (UnityEngine.Vector3)reader.ReadFromJsonUnityEngine_Vector3();
                break;
                default: return false; break;
            }
            return true;
        }
        public virtual void WriteJsonFields(ZRJsonTextWriter writer) 
        {
            writer.WritePropertyName("ancestorArray");
            ancestorArray.WriteJson(writer);
            writer.WritePropertyName("arraysAreOk");
            arraysAreOk.WriteJson(writer);
            writer.WritePropertyName("complexStructuresAreAlsoOk");
            complexStructuresAreAlsoOk.WriteJson(writer);
            writer.WritePropertyName("dictsAreOk");
            dictsAreOk.WriteJson(writer);
            writer.WritePropertyName("externalClass");
            externalClass.WriteJson(writer);
            writer.WritePropertyName("intField");
            writer.WriteValue(intField);
            writer.WritePropertyName("listsOfDataAreOk");
            listsOfDataAreOk.WriteJson(writer);
            writer.WritePropertyName("listsOfPrimitivesAreOk");
            listsOfPrimitivesAreOk.WriteJson(writer);
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
            writer.WritePropertyName("reactiveCollections");
            reactiveCollections.WriteJson(writer);
            writer.WritePropertyName("reactiveValue");
            reactiveValue.value.WriteJson(writer);
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
            writer.WritePropertyName("stringProp");
            writer.WriteValue(stringProp);
            writer.WritePropertyName("vector");
            vector.WriteJson(writer);
        }
        public virtual ushort GetClassId() 
        {
        return (System.UInt16)Types.CodeGenSamples;
        }
        public virtual System.Object NewInst() 
        {
        return new CodeGenSamples();
        }
    }
}
#endif
