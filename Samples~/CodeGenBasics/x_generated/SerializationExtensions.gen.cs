using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ZergRush.Alive;
using ZergRush;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION

public static partial class SerializationExtensions
{
    public static void UpdateFrom(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> other, ZRUpdateFromHelper __helper) 
    {
        int i = 0;
        int oldCount = self.Count;
        int crossCount = Math.Min(oldCount, other.Count);
        for (; i < crossCount; ++i)
        {
            if (other[i] == null) {
                self[i] = null;
            }
            else { 
                var self_i_ClassId = other[i].GetClassId();
                if (self[i] == null || self[i].GetClassId() != self_i_ClassId) {
                    self[i] = (ZergRush.Samples.CodeGenSamples)other[i].NewInst();
                }
                self[i].UpdateFrom(other[i], __helper);
            }
        }
        for (; i < other.Count; ++i)
        {
            ZergRush.Samples.CodeGenSamples inst = default;
            if (other[i] == null) {
                inst = null;
            }
            else { 
                inst = (ZergRush.Samples.CodeGenSamples)other[i].NewInst();
                inst.UpdateFrom(other[i], __helper);
            }
            self.Add(inst);
        }
        for (; i < oldCount; ++i)
        {
            self.RemoveAt(self.Count - 1);
        }
    }
    public static void UpdateFrom(this ZergRush.Samples.ExternalClass self, ZergRush.Samples.ExternalClass other, ZRUpdateFromHelper __helper) 
    {
        self.somePublicField = other.somePublicField;
    }
    public static void UpdateFrom(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, System.Collections.Generic.List<ZergRush.Samples.OtherData> other, ZRUpdateFromHelper __helper) 
    {
        int i = 0;
        int oldCount = self.Count;
        int crossCount = Math.Min(oldCount, other.Count);
        for (; i < crossCount; ++i)
        {
            if (other[i] == null) {
                self[i] = null;
            }
            else { 
                if (self[i] == null) {
                    self[i] = new ZergRush.Samples.OtherData();
                }
                self[i].UpdateFrom(other[i], __helper);
            }
        }
        for (; i < other.Count; ++i)
        {
            ZergRush.Samples.OtherData inst = default;
            if (other[i] == null) {
                inst = null;
            }
            else { 
                inst = new ZergRush.Samples.OtherData();
                inst.UpdateFrom(other[i], __helper);
            }
            self.Add(inst);
        }
        for (; i < oldCount; ++i)
        {
            self.RemoveAt(self.Count - 1);
        }
    }
    public static void UpdateFrom(this ZergRush.ReactiveCore.ReactiveCollection<int> self, ZergRush.ReactiveCore.ReactiveCollection<int> other, ZRUpdateFromHelper __helper) 
    {
        int i = 0;
        int oldCount = self.Count;
        int crossCount = Math.Min(oldCount, other.Count);
        for (; i < crossCount; ++i)
        {
            self[i] = other[i];
        }
        for (; i < other.Count; ++i)
        {
            var inst = other[i];
            self.Add(inst);
        }
        for (; i < oldCount; ++i)
        {
            self.RemoveAt(self.Count - 1);
        }
    }
    public static void UpdateFrom(ref this UnityEngine.Vector3 self, UnityEngine.Vector3 other, ZRUpdateFromHelper __helper) 
    {
        self.x = other.x;
        self.y = other.y;
        self.z = other.z;
    }
    public static void Deserialize(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, ZRBinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            if (!reader.ReadBoolean()) { self.Add(null); continue; }
            ZergRush.Samples.CodeGenSamples val = default;
            val = (ZergRush.Samples.CodeGenSamples)ZergRush.Samples.CodeGenSamples.CreatePolymorphic(reader.ReadUInt16());
            val.Deserialize(reader);
            self.Add(val);
        }
    }
    public static void Deserialize(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, ZRBinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        for (int i = 0; i < size; i++)
        {
            var key = default(int);
            key = reader.ReadInt32();
            if (!reader.ReadBoolean()) { self.Add(key, null); continue; }
            var val = default(System.Collections.Generic.List<System.Collections.Generic.List<string>>);
            val = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();
            val.Deserialize(reader);
            self.Add(key, val);
        }
    }
    public static void Deserialize(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, ZRBinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        for (int i = 0; i < size; i++)
        {
            var key = default(int);
            key = reader.ReadInt32();
            if (!reader.ReadBoolean()) { self.Add(key, null); continue; }
            var val = default(ZergRush.Samples.OtherData);
            val = new ZergRush.Samples.OtherData();
            val.Deserialize(reader);
            self.Add(key, val);
        }
    }
    public static void Deserialize(this ZergRush.Samples.ExternalClass self, ZRBinaryReader reader) 
    {
        self.somePublicField = reader.ReadInt32();
    }
    public static void Deserialize(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, ZRBinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            if (!reader.ReadBoolean()) { self.Add(null); continue; }
            ZergRush.Samples.OtherData val = default;
            val = new ZergRush.Samples.OtherData();
            val.Deserialize(reader);
            self.Add(val);
        }
    }
    public static void Deserialize(this ZergRush.ReactiveCore.ReactiveCollection<int> self, ZRBinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            int val = default;
            val = reader.ReadInt32();
            self.Add(val);
        }
    }
    public static UnityEngine.Vector3 ReadUnityEngine_Vector3(this ZRBinaryReader reader) 
    {
        var self = new UnityEngine.Vector3();
        self.x = reader.ReadSingle();
        self.y = reader.ReadSingle();
        self.z = reader.ReadSingle();
        return self;
    }
    public static void Serialize(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, ZRBinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            writer.Write(self[i] != null);
            if (self[i] != null)
            {
                writer.Write(self[i].GetClassId());
                self[i].Serialize(writer);
            }
        }
    }
    public static void Serialize(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, ZRBinaryWriter writer) 
    {
        writer.Write(self.Count);
        foreach (var item in self)
        {
            writer.Write(item.Key);
            writer.Write(item.Value != null);
            if (item.Value != null)
            {
                item.Value.Serialize(writer);
            }
        }
    }
    public static void Serialize(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, ZRBinaryWriter writer) 
    {
        writer.Write(self.Count);
        foreach (var item in self)
        {
            writer.Write(item.Key);
            writer.Write(item.Value != null);
            if (item.Value != null)
            {
                item.Value.Serialize(writer);
            }
        }
    }
    public static void Serialize(this ZergRush.Samples.ExternalClass self, ZRBinaryWriter writer) 
    {
        writer.Write(self.somePublicField);
    }
    public static void Serialize(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, ZRBinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            writer.Write(self[i] != null);
            if (self[i] != null)
            {
                self[i].Serialize(writer);
            }
        }
    }
    public static void Serialize(this ZergRush.ReactiveCore.ReactiveCollection<int> self, ZRBinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            {
                writer.Write(self[i]);
            }
        }
    }
    public static void Serialize(this UnityEngine.Vector3 self, ZRBinaryWriter writer) 
    {
        writer.Write(self.x);
        writer.Write(self.y);
        writer.Write(self.z);
    }
    public static ulong CalculateHash(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)910491146;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += self[i] != null ? self[i].CalculateHash(__helper) : 345093625;
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)650531450;
        hash += hash << 11; hash ^= hash >> 7;
        foreach (var item in self)
        {
            hash += (System.UInt64)item.Key;
            hash += hash << 11; hash ^= hash >> 7;
            hash += item.Value.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)650531450;
        hash += hash << 11; hash ^= hash >> 7;
        foreach (var item in self)
        {
            hash += (System.UInt64)item.Key;
            hash += hash << 11; hash ^= hash >> 7;
            hash += item.Value.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this ZergRush.Samples.ExternalClass self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)840039565;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.somePublicField;
        hash += hash << 11; hash ^= hash >> 7;
        return hash;
    }
    public static ulong CalculateHash(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)910491146;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += self[i] != null ? self[i].CalculateHash(__helper) : 345093625;
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this ZergRush.ReactiveCore.ReactiveCollection<int> self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)1261931807;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += (System.UInt64)self[i];
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this UnityEngine.Vector3 self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)701202043;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.x;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.y;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.z;
        hash += hash << 11; hash ^= hash >> 7;
        return hash;
    }
    public static void CompareCheck(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        if (self.Count != other.Count) SerializationTools.LogCompError(__helper, "Count", printer, other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            if (SerializationTools.CompareNull(__helper, i.ToString(), printer, self[i], other[i])) {
                if (SerializationTools.CompareClassId(__helper, i.ToString(), printer, self[i], other[i])) {
                    __helper.Push(i.ToString());
                    self[i].CompareCheck(other[i], __helper, printer);
                    __helper.Pop();
                }
            }
        }
    }
    public static void CompareCheck(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {

    }
    public static void CompareCheck(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {

    }
    public static void CompareCheck(this ZergRush.Samples.ExternalClass self, ZergRush.Samples.ExternalClass other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        if (self.somePublicField != other.somePublicField) SerializationTools.LogCompError(__helper, "somePublicField", printer, other.somePublicField, self.somePublicField);
    }
    public static void CompareCheck(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, System.Collections.Generic.List<ZergRush.Samples.OtherData> other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        if (self.Count != other.Count) SerializationTools.LogCompError(__helper, "Count", printer, other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            if (SerializationTools.CompareNull(__helper, i.ToString(), printer, self[i], other[i])) {
                __helper.Push(i.ToString());
                self[i].CompareCheck(other[i], __helper, printer);
                __helper.Pop();
            }
        }
    }
    public static void CompareCheck(this ZergRush.ReactiveCore.ReactiveCollection<int> self, ZergRush.ReactiveCore.ReactiveCollection<int> other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        if (self.Count != other.Count) SerializationTools.LogCompError(__helper, "Count", printer, other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            if (self[i] != other[i]) SerializationTools.LogCompError(__helper, i.ToString(), printer, other[i], self[i]);
        }
    }
    public static void CompareCheck(this UnityEngine.Vector3 self, UnityEngine.Vector3 other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        if (self.x != other.x) SerializationTools.LogCompError(__helper, "x", printer, other.x, self.x);
        if (self.y != other.y) SerializationTools.LogCompError(__helper, "y", printer, other.y, self.y);
        if (self.z != other.z) SerializationTools.LogCompError(__helper, "z", printer, other.z, self.z);
    }
    public static bool ReadFromJson(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, ZRJsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            if (reader.TokenType == JsonToken.Null) { self.Add(null); continue; }
            ZergRush.Samples.CodeGenSamples val = default;
            val = (ZergRush.Samples.CodeGenSamples)ZergRush.Samples.CodeGenSamples.CreatePolymorphic(reader.ReadJsonClassId());
            val.ReadFromJson(reader);
            self.Add(val);
        }
        return true;
    }
    public static void WriteJson(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            if (self[i] == null)
            {
                writer.WriteNull();
            }
            else
            {
                self[i].WriteJson(writer);
            }
        }
        writer.WriteEndArray();
    }
    public static bool ReadFromJson(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, ZRJsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            if (reader.TokenType != JsonToken.StartObject) throw new JsonSerializationException("Bad Json Format");
            reader.Read();
            reader.Read();
            int key = default;
            key = (int)(Int64)reader.Value;
            reader.Read();
            reader.Read();
            System.Collections.Generic.List<System.Collections.Generic.List<string>> val = default;
            val = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();
            val.ReadFromJson(reader);
            reader.ReadSkipComments();
            self.Add(key, val);
        }
        return true;
    }
    public static void WriteJson(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartArray();
        foreach (var item in self)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            writer.WriteValue(item.Key);
            writer.WritePropertyName("value");
            item.Value.WriteJson(writer);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
    public static bool ReadFromJson(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, ZRJsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            if (reader.TokenType != JsonToken.StartObject) throw new JsonSerializationException("Bad Json Format");
            reader.Read();
            reader.Read();
            int key = default;
            key = (int)(Int64)reader.Value;
            reader.Read();
            reader.Read();
            ZergRush.Samples.OtherData val = default;
            val = new ZergRush.Samples.OtherData();
            val.ReadFromJson(reader);
            reader.ReadSkipComments();
            self.Add(key, val);
        }
        return true;
    }
    public static void WriteJson(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartArray();
        foreach (var item in self)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            writer.WriteValue(item.Key);
            writer.WritePropertyName("value");
            item.Value.WriteJson(writer);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
    public static bool ReadFromJson(this ZergRush.Samples.ExternalClass self, ZRJsonTextReader reader) 
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var __name = (string) reader.Value;
                reader.Read();
                switch(__name)
                {
                    case "somePublicField":
                    self.somePublicField = (int)(Int64)reader.Value;
                    break;
                    default: return false; break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) { break; }
        }
        return true;
    }
    public static void WriteJson(this ZergRush.Samples.ExternalClass self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartObject();
        writer.WritePropertyName("somePublicField");
        writer.WriteValue(self.somePublicField);
        writer.WriteEndObject();
    }
    public static bool ReadFromJson(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, ZRJsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            if (reader.TokenType == JsonToken.Null) { self.Add(null); continue; }
            ZergRush.Samples.OtherData val = default;
            val = new ZergRush.Samples.OtherData();
            val.ReadFromJson(reader);
            self.Add(val);
        }
        return true;
    }
    public static void WriteJson(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            if (self[i] == null)
            {
                writer.WriteNull();
            }
            else
            {
                self[i].WriteJson(writer);
            }
        }
        writer.WriteEndArray();
    }
    public static bool ReadFromJson(this ZergRush.ReactiveCore.ReactiveCollection<int> self, ZRJsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            int val = default;
            val = (int)(Int64)reader.Value;
            self.Add(val);
        }
        return true;
    }
    public static void WriteJson(this ZergRush.ReactiveCore.ReactiveCollection<int> self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            writer.WriteValue(self[i]);
        }
        writer.WriteEndArray();
    }
    public static UnityEngine.Vector3 ReadFromJsonUnityEngine_Vector3(this ZRJsonTextReader reader) 
    {
        var self = new UnityEngine.Vector3();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var __name = (string) reader.Value;
                reader.Read();
                switch(__name)
                {
                    case "x":
                    self.x = (float)(double)reader.Value;
                    break;
                    case "y":
                    self.y = (float)(double)reader.Value;
                    break;
                    case "z":
                    self.z = (float)(double)reader.Value;
                    break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) { break; }
        }
        return self;
    }
    public static void WriteJson(this UnityEngine.Vector3 self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(self.x);
        writer.WritePropertyName("y");
        writer.WriteValue(self.y);
        writer.WritePropertyName("z");
        writer.WriteValue(self.z);
        writer.WriteEndObject();
    }
    public static void Deserialize(this System.Collections.Generic.List<string> self, ZRBinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            if (!reader.ReadBoolean()) { self.Add(null); continue; }
            string val = default;
            val = string.Empty;
            val = reader.ReadString();
            self.Add(val);
        }
    }
    public static void Deserialize(this System.Collections.Generic.List<System.Collections.Generic.List<string>> self, ZRBinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            if (!reader.ReadBoolean()) { self.Add(null); continue; }
            System.Collections.Generic.List<string> val = default;
            val = new System.Collections.Generic.List<string>();
            val.Deserialize(reader);
            self.Add(val);
        }
    }
    public static void Serialize(this System.Collections.Generic.List<string> self, ZRBinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            writer.Write(self[i] != null);
            if (self[i] != null)
            {
                writer.Write(self[i]);
            }
        }
    }
    public static void Serialize(this System.Collections.Generic.List<System.Collections.Generic.List<string>> self, ZRBinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            writer.Write(self[i] != null);
            if (self[i] != null)
            {
                self[i].Serialize(writer);
            }
        }
    }
    public static ulong CalculateHash(this System.Collections.Generic.List<string> self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)910491146;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += self[i] != null ? (ulong)self[i].CalculateHash() : 345093625;
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this System.Collections.Generic.List<System.Collections.Generic.List<string>> self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)910491146;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += self[i] != null ? self[i].CalculateHash(__helper) : 345093625;
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static bool ReadFromJson(this System.Collections.Generic.List<string> self, ZRJsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            if (reader.TokenType == JsonToken.Null) { self.Add(null); continue; }
            string val = default;
            val = string.Empty;
            val = (string) reader.Value;
            self.Add(val);
        }
        return true;
    }
    public static void WriteJson(this System.Collections.Generic.List<string> self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            if (self[i] == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(self[i]);
            }
        }
        writer.WriteEndArray();
    }
    public static bool ReadFromJson(this System.Collections.Generic.List<System.Collections.Generic.List<string>> self, ZRJsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            if (reader.TokenType == JsonToken.Null) { self.Add(null); continue; }
            System.Collections.Generic.List<string> val = default;
            val = new System.Collections.Generic.List<string>();
            val.ReadFromJson(reader);
            self.Add(val);
        }
        return true;
    }
    public static void WriteJson(this System.Collections.Generic.List<System.Collections.Generic.List<string>> self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            if (self[i] == null)
            {
                writer.WriteNull();
            }
            else
            {
                self[i].WriteJson(writer);
            }
        }
        writer.WriteEndArray();
    }
    public static void UpdateFrom(this System.Collections.Generic.List<string> self, System.Collections.Generic.List<string> other, ZRUpdateFromHelper __helper) 
    {
        int i = 0;
        int oldCount = self.Count;
        int crossCount = Math.Min(oldCount, other.Count);
        for (; i < crossCount; ++i)
        {
            self[i] = other[i];
        }
        for (; i < other.Count; ++i)
        {
            var inst = other[i];
            self.Add(inst);
        }
        for (; i < oldCount; ++i)
        {
            self.RemoveAt(self.Count - 1);
        }
    }
    public static void CompareCheck(this System.Collections.Generic.List<string> self, System.Collections.Generic.List<string> other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        if (self.Count != other.Count) SerializationTools.LogCompError(__helper, "Count", printer, other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            if (self[i] != other[i]) SerializationTools.LogCompError(__helper, i.ToString(), printer, other[i], self[i]);
        }
    }
}
#endif
