using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush {

    public partial class ZergRandom : IUpdatableFrom<ZergRush.ZergRandom>, IHashable, ICompareChechable<ZergRush.ZergRandom>, IJsonSerializable
    {
        public virtual void UpdateFrom(ZergRush.ZergRandom other) 
        {
            throw new NotImplementedException();
        }
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
        public virtual void CompareCheck(ZergRush.ZergRandom other, Stack<string> __path) 
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
