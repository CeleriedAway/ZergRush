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
    public static void UpdateFrom(this ZergRush.Alive.StaticConnections self, ZergRush.Alive.StaticConnections other, ZRUpdateFromHelper __helper) 
    {
        self.connections.UpdateFrom(other.connections, __helper);
        self.ownerId = other.ownerId;
    }
    public static void Deserialize(this ZergRush.Alive.StaticConnections self, ZRBinaryReader reader) 
    {
        self.connections.Deserialize(reader);
        self.ownerId = reader.ReadInt32();
    }
    public static void Serialize(this ZergRush.Alive.StaticConnections self, ZRBinaryWriter writer) 
    {
        self.connections.Serialize(writer);
        writer.Write(self.ownerId);
    }
    public static ulong CalculateHash(this ZergRush.Alive.StaticConnections self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)1300247783;
        hash += hash << 11; hash ^= hash >> 7;
        hash += self.connections.CalculateHash(__helper);
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.ownerId;
        hash += hash << 11; hash ^= hash >> 7;
        return hash;
    }
    public static void CompareCheck(this ZergRush.Alive.StaticConnections self, ZergRush.Alive.StaticConnections other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        __helper.Push("connections");
        self.connections.CompareCheck(other.connections, __helper, printer);
        __helper.Pop();
        if (self.ownerId != other.ownerId) CodeGenImplTools.LogCompError(__helper, "ownerId", printer, other.ownerId, self.ownerId);
    }
    public static bool ReadFromJson(this ZergRush.Alive.StaticConnections self, ZRJsonTextReader reader) 
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var __name = (string) reader.Value;
                reader.Read();
                switch(__name)
                {
                    case "connections":
                    self.connections.ReadFromJson(reader);
                    break;
                    case "ownerId":
                    self.ownerId = (int)(Int64)reader.Value;
                    break;
                    default: return false; break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) { break; }
        }
        return true;
    }
    public static void WriteJson(this ZergRush.Alive.StaticConnections self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartObject();
        writer.WritePropertyName("connections");
        self.connections.WriteJson(writer);
        writer.WritePropertyName("ownerId");
        writer.WriteValue(self.ownerId);
        writer.WriteEndObject();
    }
    public static void UpdateFrom(ref this ZergRush.Alive.SerializableConnection self, ZergRush.Alive.SerializableConnection other, ZRUpdateFromHelper __helper) 
    {
        self.entityId = other.entityId;
        self.ownerId = other.ownerId;
    }
    public static void UpdateFrom(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> other, ZRUpdateFromHelper __helper) 
    {
        int i = 0;
        int oldCount = self.Count;
        int crossCount = Math.Min(oldCount, other.Count);
        for (; i < crossCount; ++i)
        {
            var __self_i_ = self[i];
            __self_i_.UpdateFrom(other[i], __helper);
            self[i] = __self_i_;
        }
        for (; i < other.Count; ++i)
        {
            ZergRush.Alive.SerializableConnection inst = default;
            inst.UpdateFrom(other[i], __helper);
            self.Add(inst);
        }
        for (; i < oldCount; ++i)
        {
            self.RemoveAt(self.Count - 1);
        }
    }
    public static ZergRush.Alive.SerializableConnection ReadZergRush_Alive_SerializableConnection(this ZRBinaryReader reader) 
    {
        var self = new ZergRush.Alive.SerializableConnection();
        self.entityId = reader.ReadInt32();
        self.ownerId = reader.ReadInt32();
        return self;
    }
    public static void Deserialize(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, ZRBinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            ZergRush.Alive.SerializableConnection val = default;
            val = reader.ReadZergRush_Alive_SerializableConnection();
            self.Add(val);
        }
    }
    public static void Serialize(this ZergRush.Alive.SerializableConnection self, ZRBinaryWriter writer) 
    {
        writer.Write(self.entityId);
        writer.Write(self.ownerId);
    }
    public static void Serialize(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, ZRBinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            {
                self[i].Serialize(writer);
            }
        }
    }
    public static ulong CalculateHash(this ZergRush.Alive.SerializableConnection self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)1980239909;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.entityId;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.ownerId;
        hash += hash << 11; hash ^= hash >> 7;
        return hash;
    }
    public static ulong CalculateHash(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)910491146;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += self[i].CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static void CompareCheck(this ZergRush.Alive.SerializableConnection self, ZergRush.Alive.SerializableConnection other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        if (self.entityId != other.entityId) CodeGenImplTools.LogCompError(__helper, "entityId", printer, other.entityId, self.entityId);
        if (self.ownerId != other.ownerId) CodeGenImplTools.LogCompError(__helper, "ownerId", printer, other.ownerId, self.ownerId);
    }
    public static void CompareCheck(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        if (self.Count != other.Count) CodeGenImplTools.LogCompError(__helper, "Count", printer, other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            __helper.Push(i.ToString());
            self[i].CompareCheck(other[i], __helper, printer);
            __helper.Pop();
        }
    }
    public static ZergRush.Alive.SerializableConnection ReadFromJsonZergRush_Alive_SerializableConnection(this ZRJsonTextReader reader) 
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
                    case "entityId":
                    self.entityId = (int)(Int64)reader.Value;
                    break;
                    case "ownerId":
                    self.ownerId = (int)(Int64)reader.Value;
                    break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) { break; }
        }
        return self;
    }
    public static void WriteJson(this ZergRush.Alive.SerializableConnection self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartObject();
        writer.WritePropertyName("entityId");
        writer.WriteValue(self.entityId);
        writer.WritePropertyName("ownerId");
        writer.WriteValue(self.ownerId);
        writer.WriteEndObject();
    }
    public static bool ReadFromJson(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, ZRJsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            ZergRush.Alive.SerializableConnection val = default;
            val = (ZergRush.Alive.SerializableConnection)reader.ReadFromJsonZergRush_Alive_SerializableConnection();
            self.Add(val);
        }
        return true;
    }
    public static void WriteJson(this System.Collections.Generic.List<ZergRush.Alive.SerializableConnection> self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            self[i].WriteJson(writer);
        }
        writer.WriteEndArray();
    }
    public static void UpdateFrom(this System.Collections.Generic.List<int> self, System.Collections.Generic.List<int> other, ZRUpdateFromHelper __helper) 
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
    public static ulong CalculateHash(this System.Collections.Generic.List<int> self, ZRHashHelper __helper) 
    {
        System.UInt64 hash = 345093625;
        hash ^= (ulong)910491146;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += (System.UInt64)self[i];
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static void CompareCheck(this System.Collections.Generic.List<int> self, System.Collections.Generic.List<int> other, ZRCompareCheckHelper __helper, Action<string> printer) 
    {
        if (self.Count != other.Count) CodeGenImplTools.LogCompError(__helper, "Count", printer, other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (int i = 0; i < count; i++)
        {
            if (self[i] != other[i]) CodeGenImplTools.LogCompError(__helper, i.ToString(), printer, other[i], self[i]);
        }
    }
    public static void Deserialize(this System.Collections.Generic.List<int> self, ZRBinaryReader reader) 
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
    public static void Serialize(this System.Collections.Generic.List<int> self, ZRBinaryWriter writer) 
    {
        writer.Write(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            {
                writer.Write(self[i]);
            }
        }
    }
    public static bool ReadFromJson(this System.Collections.Generic.List<int> self, ZRJsonTextReader reader) 
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
    public static void WriteJson(this System.Collections.Generic.List<int> self, ZRJsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            writer.WriteValue(self[i]);
        }
        writer.WriteEndArray();
    }
}
#endif
