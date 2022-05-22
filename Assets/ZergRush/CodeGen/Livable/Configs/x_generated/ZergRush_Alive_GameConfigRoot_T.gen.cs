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
            throw new NotImplementedException();
        }
        public virtual void Serialize(BinaryWriter writer) 
        {
            throw new NotImplementedException();
        }
        public virtual ulong CalculateHash() 
        {
            throw new NotImplementedException();
        }
        public virtual void CollectConfigs(ConfigRegister _collection) 
        {
            throw new NotImplementedException();
        }
        public  GameConfigRoot() 
        {
            throw new NotImplementedException();
        }
        public virtual void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            throw new NotImplementedException();
        }
        public virtual void WriteJsonFields(JsonTextWriter writer) 
        {
            throw new NotImplementedException();
        }
    }
}
#endif
