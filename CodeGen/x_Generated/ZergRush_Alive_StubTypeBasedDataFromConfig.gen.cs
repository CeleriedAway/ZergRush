using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class StubTypeBasedDataFromConfig : IHashable, IUniquelyIdentifiable, IJsonSerializable
    {
        public override void Deserialize(BinaryReader reader) 
        {
            base.Deserialize(reader);

        }
        public override void Serialize(BinaryWriter writer) 
        {
            base.Serialize(writer);

        }
        public override ulong CalculateHash() 
        {
            var baseVal = base.CalculateHash();
            System.UInt64 hash = baseVal;
            hash += (ulong)832556751;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public override ulong UId() 
        {
            System.UInt64 hash = 345093625;
            return hash;
        }
        public override void CollectConfigs(ConfigRegister _collection) 
        {
            base.CollectConfigs(_collection);

        }
        public  StubTypeBasedDataFromConfig() 
        {

        }
        public override void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            base.ReadFromJsonField(reader,__name);
            switch(__name)
            {
            }
        }
        public override void WriteJsonFields(JsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);

        }
    }
}
#endif
