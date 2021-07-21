using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ZergRush.Alive;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION

public static partial class SerializationExtensions
{
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
