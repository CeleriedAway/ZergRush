using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class GameConfigExample : IHashable, IJsonSerializable, IPolymorphable
    {
        public override void Deserialize(BinaryReader reader) 
        {
            base.Deserialize(reader);
            examples.Deserialize(reader);
        }
        public override void Serialize(BinaryWriter writer) 
        {
            base.Serialize(writer);
            examples.Serialize(writer);
        }
        public override ulong CalculateHash() 
        {
            var baseVal = base.CalculateHash();
            System.UInt64 hash = baseVal;
            hash += (ulong)993987480;
            hash += hash << 11; hash ^= hash >> 7;
            hash += examples.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public override void CollectConfigs(ConfigRegister _collection) 
        {
            base.CollectConfigs(_collection);
            examples.CollectConfigs(_collection);
        }
        public  GameConfigExample() 
        {
            examples = new ZergRush.Alive.ConfigStorageList<ZergRush.Alive.GameLoadableConfigMemberExample>();
        }
        public override void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            base.ReadFromJsonField(reader,__name);
            switch(__name)
            {
                case "examples":
                examples.ReadFromJson(reader);
                break;
            }
        }
        public override void WriteJsonFields(JsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);
            writer.WritePropertyName("examples");
            examples.WriteJson(writer);
        }
        public override ushort GetClassId() 
        {
        return (System.UInt16)Types.GameConfigExample;
        }
        public override ZergRush.Alive.__GameConfigBase NewInst() 
        {
        return new GameConfigExample();
        }
    }
}
#endif
