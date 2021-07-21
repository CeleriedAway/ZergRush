using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ZergRush.Alive;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION

public static partial class SerializationExtensions
{
    public static void ReadFromJson(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, JsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            ZergRush.Samples.CodeGenSamples val = default;
            val = (ZergRush.Samples.CodeGenSamples)ZergRush.Samples.CodeGenSamples.CreatePolymorphic(reader.ReadJsonClassId());
            val.ReadFromJson(reader);
            self.Add(val);
        }
    }
    public static void WriteJson(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, JsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            self[i].WriteJson(writer);
        }
        writer.WriteEndArray();
    }
    public static void ReadFromJson(this ZergRush.ReactiveCore.ReactiveCollection<int> self, JsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            int val = default;
            val = (int)(Int64)reader.Value;
            self.Add(val);
        }
    }
    public static void WriteJson(this ZergRush.ReactiveCore.ReactiveCollection<int> self, JsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            writer.WriteValue(self[i]);
        }
        writer.WriteEndArray();
    }
    public static void ReadFromJson(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, JsonTextReader reader) 
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
            reader.Read();
            self.Add(key, val);
        }
    }
    public static void WriteJson(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, JsonTextWriter writer) 
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
    public static void ReadFromJson(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, JsonTextReader reader) 
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
            reader.Read();
            self.Add(key, val);
        }
    }
    public static void WriteJson(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, JsonTextWriter writer) 
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
    public static void ReadFromJson(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, JsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            ZergRush.Samples.OtherData val = default;
            val = new ZergRush.Samples.OtherData();
            val.ReadFromJson(reader);
            self.Add(val);
        }
    }
    public static void WriteJson(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, JsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            self[i].WriteJson(writer);
        }
        writer.WriteEndArray();
    }
    public static UnityEngine.Vector3 ReadFromJsonUnityEngine_Vector3(this JsonTextReader reader) 
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
                    self.x = (System.Single)(double)reader.Value;
                    break;
                    case "y":
                    self.y = (System.Single)(double)reader.Value;
                    break;
                    case "z":
                    self.z = (System.Single)(double)reader.Value;
                    break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) { break; }
        }
        return self;
    }
    public static void WriteJson(this UnityEngine.Vector3 self, JsonTextWriter writer) 
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
    public static void ReadFromJson(this ZergRush.Samples.ExternalClass self, JsonTextReader reader) 
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
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) { break; }
        }
    }
    public static void WriteJson(this ZergRush.Samples.ExternalClass self, JsonTextWriter writer) 
    {
        writer.WriteStartObject();
        writer.WritePropertyName("somePublicField");
        writer.WriteValue(self.somePublicField);
        writer.WriteEndObject();
    }
    public static void CompareCheck(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> other, Stack<string> __path) 
    {
        if (self.Count != other.Count) SerializationTools.LogCompError(__path, "Count", other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            if (SerializationTools.CompareClassId(__path, i.ToString(), self[i], other[i])) {
                __path.Push(i.ToString());
                self[i].CompareCheck(other[i], __path);
                __path.Pop();
            }
        }
    }
    public static void CompareCheck(this ZergRush.ReactiveCore.ReactiveCollection<int> self, ZergRush.ReactiveCore.ReactiveCollection<int> other, Stack<string> __path) 
    {
        if (self.Count != other.Count) SerializationTools.LogCompError(__path, "Count", other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            if (self[i] != other[i]) SerializationTools.LogCompError(__path, i.ToString(), other[i], self[i]);
        }
    }
    public static void CompareCheck(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> other, Stack<string> __path) 
    {

    }
    public static void CompareCheck(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> other, Stack<string> __path) 
    {

    }
    public static void CompareCheck(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, System.Collections.Generic.List<ZergRush.Samples.OtherData> other, Stack<string> __path) 
    {
        if (self.Count != other.Count) SerializationTools.LogCompError(__path, "Count", other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            __path.Push(i.ToString());
            self[i].CompareCheck(other[i], __path);
            __path.Pop();
        }
    }
    public static void CompareCheck(this UnityEngine.Vector3 self, UnityEngine.Vector3 other, Stack<string> __path) 
    {
        if (self.x != other.x) SerializationTools.LogCompError(__path, "x", other.x, self.x);
        if (self.y != other.y) SerializationTools.LogCompError(__path, "y", other.y, self.y);
        if (self.z != other.z) SerializationTools.LogCompError(__path, "z", other.z, self.z);
    }
    public static void CompareCheck(this ZergRush.Samples.ExternalClass self, ZergRush.Samples.ExternalClass other, Stack<string> __path) 
    {
        if (self.somePublicField != other.somePublicField) SerializationTools.LogCompError(__path, "somePublicField", other.somePublicField, self.somePublicField);
    }
    public static ulong CalculateHash(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)340019353;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += self[i].CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this ZergRush.ReactiveCore.ReactiveCollection<int> self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)590981122;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += (System.UInt64)self[i];
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)30524776;
        hash += hash << 11; hash ^= hash >> 7;
        foreach (var item in self)
        {
            hash += (System.UInt64)item.Key;
            hash += hash << 11; hash ^= hash >> 7;
            hash += item.Value.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)30524776;
        hash += hash << 11; hash ^= hash >> 7;
        foreach (var item in self)
        {
            hash += (System.UInt64)item.Key;
            hash += hash << 11; hash ^= hash >> 7;
            hash += item.Value.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)340019353;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += self[i].CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static ulong CalculateHash(this UnityEngine.Vector3 self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)1335531434;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.x;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.y;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.z;
        hash += hash << 11; hash ^= hash >> 7;
        return hash;
    }
    public static ulong CalculateHash(this ZergRush.Samples.ExternalClass self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)1236288241;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.somePublicField;
        hash += hash << 11; hash ^= hash >> 7;
        return hash;
    }
    public static void Serialize(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, BinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            writer.Write(self[i].GetClassId());
            self[i].Serialize(writer);
        }
    }
    public static void Serialize(this ZergRush.ReactiveCore.ReactiveCollection<int> self, BinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            writer.Write(self[i]);
        }
    }
    public static void Serialize(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, BinaryWriter writer) 
    {
        writer.Write(self.Count);
        foreach (var item in self)
        {
            writer.Write(item.Key);
            item.Value.Serialize(writer);
        }
    }
    public static void Serialize(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, BinaryWriter writer) 
    {
        writer.Write(self.Count);
        foreach (var item in self)
        {
            writer.Write(item.Key);
            item.Value.Serialize(writer);
        }
    }
    public static void Serialize(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, BinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            self[i].Serialize(writer);
        }
    }
    public static void Serialize(this UnityEngine.Vector3 self, BinaryWriter writer) 
    {
        writer.Write(self.x);
        writer.Write(self.y);
        writer.Write(self.z);
    }
    public static void Serialize(this ZergRush.Samples.ExternalClass self, BinaryWriter writer) 
    {
        writer.Write(self.somePublicField);
    }
    public static void Deserialize(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, BinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 1000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            ZergRush.Samples.CodeGenSamples val = default;
            val = (ZergRush.Samples.CodeGenSamples)ZergRush.Samples.CodeGenSamples.CreatePolymorphic(reader.ReadUInt16());
            val.Deserialize(reader);
            self.Add(val);
        }
    }
    public static void Deserialize(this ZergRush.ReactiveCore.ReactiveCollection<int> self, BinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 1000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            int val = default;
            val = reader.ReadInt32();
            self.Add(val);
        }
    }
    public static void Deserialize(this System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<System.Collections.Generic.List<string>>> self, BinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 1000) throw new ZergRushCorruptedOrInvalidDataLayout();
        for (int i = 0; i < size; i++)
        {
            var key = default(int);
            key = reader.ReadInt32();
            var val = default(System.Collections.Generic.List<System.Collections.Generic.List<string>>);
            val = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();
            val.Deserialize(reader);
            self.Add(key, val);
        }
    }
    public static void Deserialize(this System.Collections.Generic.Dictionary<int, ZergRush.Samples.OtherData> self, BinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 1000) throw new ZergRushCorruptedOrInvalidDataLayout();
        for (int i = 0; i < size; i++)
        {
            var key = default(int);
            key = reader.ReadInt32();
            var val = default(ZergRush.Samples.OtherData);
            val = new ZergRush.Samples.OtherData();
            val.Deserialize(reader);
            self.Add(key, val);
        }
    }
    public static void Deserialize(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, BinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 1000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            ZergRush.Samples.OtherData val = default;
            val = new ZergRush.Samples.OtherData();
            val.Deserialize(reader);
            self.Add(val);
        }
    }
    public static UnityEngine.Vector3 ReadUnityEngine_Vector3(this BinaryReader reader) 
    {
        var self = new UnityEngine.Vector3();
        self.x = reader.ReadSingle();
        self.y = reader.ReadSingle();
        self.z = reader.ReadSingle();
        return self;
    }
    public static void Deserialize(this ZergRush.Samples.ExternalClass self, BinaryReader reader) 
    {
        self.somePublicField = reader.ReadInt32();
    }
    public static void UpdateFrom(this System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> self, System.Collections.Generic.List<ZergRush.Samples.CodeGenSamples> other) 
    {
        int i = 0;
        int oldCount = self.Count;
        int crossCount = Math.Min(oldCount, other.Count);
        for (; i < crossCount; ++i)
        {
            var self_i_ClassId = other[i].GetClassId();
            if (self[i] == null || self[i].GetClassId() != self_i_ClassId) {
                self[i] = (ZergRush.Samples.CodeGenSamples)other[i].NewInst();
            }
            self[i].UpdateFrom(other[i]);
        }
        for (; i < other.Count; ++i)
        {
            ZergRush.Samples.CodeGenSamples inst = default;
            inst = (ZergRush.Samples.CodeGenSamples)other[i].NewInst();
            inst.UpdateFrom(other[i]);
            self.Add(inst);
        }
        for (; i < oldCount; ++i)
        {
            self.RemoveAt(self.Count - 1);
        }
    }
    public static void UpdateFrom(this ZergRush.ReactiveCore.ReactiveCollection<int> self, ZergRush.ReactiveCore.ReactiveCollection<int> other) 
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
    public static void UpdateFrom(this System.Collections.Generic.List<ZergRush.Samples.OtherData> self, System.Collections.Generic.List<ZergRush.Samples.OtherData> other) 
    {
        int i = 0;
        int oldCount = self.Count;
        int crossCount = Math.Min(oldCount, other.Count);
        for (; i < crossCount; ++i)
        {
            self[i].UpdateFrom(other[i]);
        }
        for (; i < other.Count; ++i)
        {
            ZergRush.Samples.OtherData inst = default;
            inst = new ZergRush.Samples.OtherData();
            inst.UpdateFrom(other[i]);
            self.Add(inst);
        }
        for (; i < oldCount; ++i)
        {
            self.RemoveAt(self.Count - 1);
        }
    }
    public static void UpdateFrom(this UnityEngine.Vector3 self, UnityEngine.Vector3 other) 
    {
        self.x = other.x;
        self.y = other.y;
        self.z = other.z;
    }
    public static void UpdateFrom(this ZergRush.Samples.ExternalClass self, ZergRush.Samples.ExternalClass other) 
    {
        self.somePublicField = other.somePublicField;
    }
}
#endif
