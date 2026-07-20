using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public static partial class SerializationExtensions
{
    public static void UpdateFrom(this int[] self, int[] other, ZergRush.ZRUpdateFromHelper helper)
    {
        for (var i = 0; i < self.Length; i++) self[i] = other[i];
    }

    public static int[] ReadSystem_Int32_Array(this ZergRush.ZRBinaryReader reader)
    {
        var size = reader.ReadInt32();
        if (size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        var array = new int[size];
        for (var i = 0; i < size; i++) array[i] = reader.ReadInt32();
        return array;
    }

    public static void Serialize(this int[] self, ZergRush.ZRBinaryWriter writer)
    {
        writer.Write(self.Length);
        for (var i = 0; i < self.Length; i++) writer.Write(self[i]);
    }

    public static ulong CalculateHash(this int[] self, ZergRush.ZRHashHelper helper)
    {
        ulong hash = 345093625;
        hash ^= 677530667;
        hash += hash << 11;
        hash ^= hash >> 7;
        for (var i = 0; i < self.Length; i++)
        {
            hash += (ulong)self[i];
            hash += hash << 11;
            hash ^= hash >> 7;
        }
        return hash;
    }

    public static void CompareCheck(this int[] self, int[] other, ZergRush.ZRCompareCheckHelper helper,
        Action<string> printer)
    {
        if (self.Length != other.Length)
            CodeGenImplTools.LogCompError(helper, "Length", printer, other.Length, self.Length);
        var count = Math.Min(self.Length, other.Length);
        for (var i = 0; i < count; i++)
        {
            if (self[i] != other[i])
                CodeGenImplTools.LogCompError(helper, i.ToString(), printer, other[i], self[i]);
        }
    }

    public static int[] ReadFromJson(this int[] self, ZergRush.ZRJsonTextReader reader)
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        if (self == null || self.Length > 0) self = Array.Empty<int>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) break;
            Array.Resize(ref self, self.Length + 1);
            self[self.Length - 1] = (int)(long)reader.Value;
        }
        return self;
    }

    public static void WriteJson(this int[] self, ZergRush.ZRJsonTextWriter writer)
    {
        writer.WriteStartArray();
        for (var i = 0; i < self.Length; i++) writer.WriteValue(self[i]);
        writer.WriteEndArray();
    }

    public static void UpdateFrom(this List<int> self, List<int> other, ZergRush.ZRUpdateFromHelper helper)
    {
        var commonCount = Math.Min(self.Count, other.Count);
        for (var i = 0; i < commonCount; i++) self[i] = other[i];
        for (var i = commonCount; i < other.Count; i++) self.Add(other[i]);
        while (self.Count > other.Count) self.RemoveAt(self.Count - 1);
    }

    public static void Deserialize(this List<int> self, ZergRush.ZRBinaryReader reader)
    {
        var size = reader.ReadInt32();
        if (size > 100000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = Math.Max(self.Capacity, size);
        for (var i = 0; i < size; i++) self.Add(reader.ReadInt32());
    }

    public static void Serialize(this List<int> self, ZergRush.ZRBinaryWriter writer)
    {
        writer.Write(self.Count);
        for (var i = 0; i < self.Count; i++) writer.Write(self[i]);
    }

    public static ulong CalculateHash(this List<int> self, ZergRush.ZRHashHelper helper)
    {
        ulong hash = 345093625;
        hash ^= 910491146;
        hash += hash << 11;
        hash ^= hash >> 7;
        for (var i = 0; i < self.Count; i++)
        {
            hash += (ulong)self[i];
            hash += hash << 11;
            hash ^= hash >> 7;
        }
        return hash;
    }

    public static void CompareCheck(this List<int> self, List<int> other,
        ZergRush.ZRCompareCheckHelper helper, Action<string> printer)
    {
        if (self.Count != other.Count)
            CodeGenImplTools.LogCompError(helper, "Count", printer, other.Count, self.Count);
        var count = Math.Min(self.Count, other.Count);
        for (var i = 0; i < count; i++)
        {
            if (self[i] != other[i])
                CodeGenImplTools.LogCompError(helper, i.ToString(), printer, other[i], self[i]);
        }
    }

    public static bool ReadFromJson(this List<int> self, ZergRush.ZRJsonTextReader reader)
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) break;
            self.Add((int)(long)reader.Value);
        }
        return true;
    }

    public static void WriteJson(this List<int> self, ZergRush.ZRJsonTextWriter writer)
    {
        writer.WriteStartArray();
        for (var i = 0; i < self.Count; i++) writer.WriteValue(self[i]);
        writer.WriteEndArray();
    }
}
