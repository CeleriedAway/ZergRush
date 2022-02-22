using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class GameConfigRoot<T> : IHashable, IJsonSerializable
    {
        public virtual void Deserialize(BinaryReader reader) 
        {
            version = reader.ReadZergRush_Alive_Version();
        }
        public virtual void Serialize(BinaryWriter writer) 
        {
            version.Serialize(writer);
        }
        public virtual ulong CalculateHash() 
        {
            System.UInt64 hash = 345093625;
            hash += version.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public virtual void CollectConfigs(ConfigRegister _collection) 
        {

        }
        public  GameConfigRoot() 
        {

        }
        public virtual void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "version":
                version = (ZergRush.Alive.Version)reader.ReadFromJsonZergRush_Alive_Version();
                break;
            }
        }
        public virtual void WriteJsonFields(JsonTextWriter writer) 
        {
            writer.WritePropertyName("version");
            version.WriteJson(writer);
        }
    }
}
#endif
