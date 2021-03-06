using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ZergRush.Alive;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION

public static partial class SerializationExtensions
{
    public static void ReadFromJson(this System.Collections.Generic.List<int> self, JsonTextReader reader) 
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
    public static void WriteJson(this System.Collections.Generic.List<int> self, JsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            writer.WriteValue(self[i]);
        }
        writer.WriteEndArray();
    }
    public static void Serialize(this System.Collections.Generic.List<int> self, BinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            writer.Write(self[i]);
        }
    }
    public static void Deserialize(this System.Collections.Generic.List<int> self, BinaryReader reader) 
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
    public static void ReadFromJson(this ZergRush.Alive.StaticConnections self, JsonTextReader reader) 
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var __name = (string) reader.Value;
                reader.Read();
                switch(__name)
                {
                    case "ownerId":
                    self.ownerId = (int)(Int64)reader.Value;
                    break;
                    case "connections":
                    self.connections.ReadFromJson(reader);
                    break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) { break; }
        }
    }
    public static void WriteJson(this ZergRush.Alive.StaticConnections self, JsonTextWriter writer) 
    {
        writer.WriteStartObject();
        writer.WritePropertyName("ownerId");
        writer.WriteValue(self.ownerId);
        writer.WritePropertyName("connections");
        self.connections.WriteJson(writer);
        writer.WriteEndObject();
    }
    public static void ReadFromJson(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, JsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            ZergRush.Alive.SerializableConnection val = default;
            val = (ZergRush.Alive.SerializableConnection)reader.ReadFromJsonZergRush_Alive_SerializableConnection();
            self.Add(val);
        }
    }
    public static void WriteJson(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, JsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            self[i].WriteJson(writer);
        }
        writer.WriteEndArray();
    }
    public static ZergRush.Alive.SerializableConnection ReadFromJsonZergRush_Alive_SerializableConnection(this JsonTextReader reader) 
    {
        var self = new ZergRush.Alive.SerializableConnection();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var __name = (string) reader.Value;
                reader.Read();
                switch(__name)
                {
                    case "ownerId":
                    self.ownerId = (int)(Int64)reader.Value;
                    break;
                    case "entityId":
                    self.entityId = (int)(Int64)reader.Value;
                    break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) { break; }
        }
        return self;
    }
    public static void WriteJson(this ZergRush.Alive.SerializableConnection self, JsonTextWriter writer) 
    {
        writer.WriteStartObject();
        writer.WritePropertyName("ownerId");
        writer.WriteValue(self.ownerId);
        writer.WritePropertyName("entityId");
        writer.WriteValue(self.entityId);
        writer.WriteEndObject();
    }
    public static void CompareCheck(this ZergRush.Alive.StaticConnections self, ZergRush.Alive.StaticConnections other, Stack<string> __path) 
    {
        if (self.ownerId != other.ownerId) SerializationTools.LogCompError(__path, "ownerId", other.ownerId, self.ownerId);
        __path.Push("connections");
        self.connections.CompareCheck(other.connections, __path);
        __path.Pop();
    }
    public static void CompareCheck(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> other, Stack<string> __path) 
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
    public static void CompareCheck(this ZergRush.Alive.SerializableConnection self, ZergRush.Alive.SerializableConnection other, Stack<string> __path) 
    {
        if (self.ownerId != other.ownerId) SerializationTools.LogCompError(__path, "ownerId", other.ownerId, self.ownerId);
        if (self.entityId != other.entityId) SerializationTools.LogCompError(__path, "entityId", other.entityId, self.entityId);
    }
    public static ulong CalculateHash(this ZergRush.Alive.StaticConnections self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)1002808563;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.ownerId;
        hash += hash << 11; hash ^= hash >> 7;
        hash += self.connections.CalculateHash();
        hash += hash << 11; hash ^= hash >> 7;
        return hash;
    }
    public static ulong CalculateHash(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self) 
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
    public static ulong CalculateHash(this ZergRush.Alive.SerializableConnection self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)1598172611;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.ownerId;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.entityId;
        hash += hash << 11; hash ^= hash >> 7;
        return hash;
    }
    public static void Serialize(this ZergRush.Alive.StaticConnections self, BinaryWriter writer) 
    {
        writer.Write(self.ownerId);
        self.connections.Serialize(writer);
    }
    public static void Serialize(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, BinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            self[i].Serialize(writer);
        }
    }
    public static void Serialize(this ZergRush.Alive.SerializableConnection self, BinaryWriter writer) 
    {
        writer.Write(self.ownerId);
        writer.Write(self.entityId);
    }
    public static void Deserialize(this ZergRush.Alive.StaticConnections self, BinaryReader reader) 
    {
        self.ownerId = reader.ReadInt32();
        self.connections.Deserialize(reader);
    }
    public static void Deserialize(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, BinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 1000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            ZergRush.Alive.SerializableConnection val = default;
            val = reader.ReadZergRush_Alive_SerializableConnection();
            self.Add(val);
        }
    }
    public static ZergRush.Alive.SerializableConnection ReadZergRush_Alive_SerializableConnection(this BinaryReader reader) 
    {
        var self = new ZergRush.Alive.SerializableConnection();
        self.ownerId = reader.ReadInt32();
        self.entityId = reader.ReadInt32();
        return self;
    }
    public static void UpdateFrom(this ZergRush.Alive.StaticConnections self, ZergRush.Alive.StaticConnections other) 
    {
        self.ownerId = other.ownerId;
        self.connections.UpdateFrom(other.connections);
    }
    public static void UpdateFrom(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> other) 
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
    public static void UpdateFrom(this ZergRush.Alive.SerializableConnection self, ZergRush.Alive.SerializableConnection other) 
    {
        self.ownerId = other.ownerId;
        self.entityId = other.entityId;
    }
    public static void CompareCheck(this System.Collections.Generic.List<int> self, System.Collections.Generic.List<int> other, Stack<string> __path) 
    {
        if (self.Count != other.Count) SerializationTools.LogCompError(__path, "Count", other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            if (self[i] != other[i]) SerializationTools.LogCompError(__path, i.ToString(), other[i], self[i]);
        }
    }
    public static ulong CalculateHash(this System.Collections.Generic.List<int> self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)340019353;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += (System.UInt64)self[i];
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static void UpdateFrom(this System.Collections.Generic.List<int> self, System.Collections.Generic.List<int> other) 
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
    public static System.Int32[] ReadFromJson(this System.Int32[] self, JsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        if(self == null || self.Length > 0) self = Array.Empty<int>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            int val = default;
            val = (int)(Int64)reader.Value;
            Array.Resize(ref self, self.Length + 1);
            self[self.Length - 1] = val;
        }
        return self;
    }
    public static void WriteJson(this System.Int32[] self, JsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Length; i++)
        {
            writer.WriteValue(self[i]);
        }
        writer.WriteEndArray();
    }
    public static void CompareCheck(this System.Int32[] self, System.Int32[] other, Stack<string> __path) 
    {
        if (self.Length != other.Length) SerializationTools.LogCompError(__path, "Length", other.Length, self.Length);
        var count = Math.Min(self.Length, other.Length);
        for (int i = 0; i < count; i++)
        {
            if (self[i] != other[i]) SerializationTools.LogCompError(__path, i.ToString(), other[i], self[i]);
        }
    }
    public static ulong CalculateHash(this System.Int32[] self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)546861222;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Length;
        for (int i = 0; i < size; i++)
        {
            hash += (System.UInt64)self[i];
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static void Serialize(this System.Int32[] self, BinaryWriter writer) 
    {
        writer.Write(self.Length);
        for (int i = 0; i < self.Length; i++)
        {
            writer.Write(self[i]);
        }
    }
    public static System.Int32[] ReadSystem_Int32_Array(this BinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 1000) throw new ZergRushCorruptedOrInvalidDataLayout();
        var array = new int[size];
        for (int i = 0; i < size; i++)
        {
            array[i] = reader.ReadInt32();
        }
        return array;
    }
    public static void UpdateFrom(this System.Int32[] self, System.Int32[] other) 
    {
        for (int i = 0; i < self.Length; i++)
        {
            self[i] = other[i];
        }
    }
}
#endif
