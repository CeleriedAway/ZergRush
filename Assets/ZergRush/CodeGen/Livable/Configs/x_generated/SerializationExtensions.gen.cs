using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ZergRush.Alive;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION

public static partial class SerializationExtensions
{
    public static void Deserialize(this ZergRush.Alive.ConfigStorageList<ZergRush.Alive.SomeItemFromConfig> self, BinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 1000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
            if (!reader.ReadBoolean()) { self.Add(null); continue; }
            ZergRush.Alive.SomeItemFromConfig val = default;
            val = new ZergRush.Alive.SomeItemFromConfig();
            val.Deserialize(reader);
            ZergRush.Alive.GameConfigExample.Instance.RegisterConfig(val);
            self.Add(val);
        }
    }
    public static void Serialize(this ZergRush.Alive.ConfigStorageList<ZergRush.Alive.SomeItemFromConfig> self, BinaryWriter writer) 
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
    public static ulong CalculateHash(this ZergRush.Alive.ConfigStorageList<ZergRush.Alive.SomeItemFromConfig> self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)2110108542;
        hash += hash << 11; hash ^= hash >> 7;
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            hash += self[i] != null ? (ulong)self[i].UId() : 345093625;
            hash += hash << 11; hash ^= hash >> 7;
        }
        return hash;
    }
    public static void CollectConfigs(this ZergRush.Alive.ConfigStorageList<ZergRush.Alive.SomeItemFromConfig> self, ConfigRegister _collection) 
    {
        var size = self.Count;
        for (int i = 0; i < size; i++)
        {
            _collection.AddConfigToRegister(self[i]);
            self[i]?.CollectConfigs(_collection);
        }
    }
    public static void ReadFromJson(this ZergRush.Alive.ConfigStorageList<ZergRush.Alive.SomeItemFromConfig> self, JsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
            if (reader.TokenType == JsonToken.Null) { self.Add(null); continue; }
            ZergRush.Alive.SomeItemFromConfig val = default;
            val = new ZergRush.Alive.SomeItemFromConfig();
            val.ReadFromJson(reader);
            ZergRush.Alive.GameConfigExample.Instance.RegisterConfig(val);
            self.Add(val);
        }
    }
    public static void WriteJson(this ZergRush.Alive.ConfigStorageList<ZergRush.Alive.SomeItemFromConfig> self, JsonTextWriter writer) 
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            self[i].WriteJson(writer);
        }
        writer.WriteEndArray();
    }
}
#endif
