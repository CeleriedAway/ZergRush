using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class SomeItemFromConfig : IBinaryDeserializable, IBinarySerializable, IHashable, IJsonSerializable
    {
        public override void Deserialize(BinaryReader reader) 
        {
            base.Deserialize(reader);
            id = reader.ReadString();
            price = reader.ReadInt32();
            name = reader.ReadString();
        }
        public override void Serialize(BinaryWriter writer) 
        {
            base.Serialize(writer);
            writer.Write(id);
            writer.Write(price);
            writer.Write(name);
        }
        public override ulong CalculateHash() 
        {
            var baseVal = base.CalculateHash();
            System.UInt64 hash = baseVal;
            hash += (ulong)72721043;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (ulong)id.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)price;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (ulong)name.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public override void CollectConfigs(ConfigRegister _collection) 
        {
            base.CollectConfigs(_collection);

        }
        public  SomeItemFromConfig() 
        {
            id = string.Empty;
            name = string.Empty;
        }
        public override bool ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            if (base.ReadFromJsonField(reader, __name)) return true;
            switch(__name)
            {
                case "id":
                id = (string) reader.Value;
                break;
                case "price":
                price = (int)(Int64)reader.Value;
                break;
                case "name":
                name = (string) reader.Value;
                break;
                default: return false; break;
            }
            return true;
        }
        public override void WriteJsonFields(JsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);
            writer.WritePropertyName("id");
            writer.WriteValue(id);
            writer.WritePropertyName("price");
            writer.WriteValue(price);
            writer.WritePropertyName("name");
            writer.WriteValue(name);
        }
    }
}
#endif
