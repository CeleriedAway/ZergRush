using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class GameConfigExample : IHashable, IJsonSerializable
    {
        public override void Deserialize(BinaryReader reader) 
        {
            base.Deserialize(reader);
            items.Deserialize(reader);
        }
        public override void Serialize(BinaryWriter writer) 
        {
            base.Serialize(writer);
            items.Serialize(writer);
        }
        public override ulong CalculateHash() 
        {
            var baseVal = base.CalculateHash();
            System.UInt64 hash = baseVal;
            hash += (ulong)178743089;
            hash += hash << 11; hash ^= hash >> 7;
            hash += items.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public override void CollectConfigs(ConfigRegister _collection) 
        {
            base.CollectConfigs(_collection);
            items.CollectConfigs(_collection);
        }
        public  GameConfigExample() 
        {
            items = new ZergRush.Alive.ConfigStorageList<ZergRush.Alive.SomeItemFromConfig>();
        }
        public override void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            base.ReadFromJsonField(reader,__name);
            switch(__name)
            {
                case "items":
                items.ReadFromJson(reader);
                break;
            }
        }
        public override void WriteJsonFields(JsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);
            writer.WritePropertyName("items");
            items.WriteJson(writer);
        }
    }
}
#endif
