using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ZergRush.Alive;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION

public static partial class SerializationExtensions
{
    public static ZergRush.Alive.Version ReadZergRush_Alive_Version(this BinaryReader reader) 
    {
        var self = new ZergRush.Alive.Version();
        self.major = reader.ReadInt32();
        self.middle = reader.ReadInt32();
        self.minor = reader.ReadInt32();
        return self;
    }
    public static void Serialize(this ZergRush.Alive.Version self, BinaryWriter writer) 
    {
        writer.Write(self.major);
        writer.Write(self.middle);
        writer.Write(self.minor);
    }
    public static ulong CalculateHash(this ZergRush.Alive.Version self) 
    {
        System.UInt64 hash = 345093625;
        hash += (ulong)4699698;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.major;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.middle;
        hash += hash << 11; hash ^= hash >> 7;
        hash += (System.UInt64)self.minor;
        hash += hash << 11; hash ^= hash >> 7;
        return hash;
    }
    public static ZergRush.Alive.Version ReadFromJsonZergRush_Alive_Version(this JsonTextReader reader) 
    {
        var self = new ZergRush.Alive.Version();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var __name = (string) reader.Value;
                reader.Read();
                switch(__name)
                {
                    case "major":
                    self.major = (int)(Int64)reader.Value;
                    break;
                    case "middle":
                    self.middle = (int)(Int64)reader.Value;
                    break;
                    case "minor":
                    self.minor = (int)(Int64)reader.Value;
                    break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject) { break; }
        }
        return self;
    }
    public static void WriteJson(this ZergRush.Alive.Version self, JsonTextWriter writer) 
    {
        writer.WriteStartObject();
        writer.WritePropertyName("major");
        writer.WriteValue(self.major);
        writer.WritePropertyName("middle");
        writer.WriteValue(self.middle);
        writer.WritePropertyName("minor");
        writer.WriteValue(self.minor);
        writer.WriteEndObject();
    }
    public static void Deserialize(this ZergRush.Alive.ConfigStorageList<ZergRush.Alive.SomeItemFromConfig> self, BinaryReader reader) 
    {
        var size = reader.ReadInt32();
        if(size > 1000) throw new ZergRushCorruptedOrInvalidDataLayout();
        self.Capacity = size;
        for (int i = 0; i < size; i++)
        {
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
            self[i].Serialize(writer);
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
            hash += (ulong)self[i].UId();
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
            self[i].CollectConfigs(_collection);
        }
    }
    public static void ReadFromJson(this ZergRush.Alive.ConfigStorageList<ZergRush.Alive.SomeItemFromConfig> self, JsonTextReader reader) 
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) { break; }
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
