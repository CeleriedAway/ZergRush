using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class DataNode : IUpdatableFrom<ZergRush.Alive.DataNode>, IHashable, ICompareChechable<ZergRush.Alive.DataNode>, IJsonSerializable
    {
        public virtual void UpdateFrom(ZergRush.Alive.DataNode other) 
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
        public virtual void __GenIds(DataRoot __root) 
        {
            throw new NotImplementedException();
        }
        public virtual void __PropagateHierarchyAndRememberIds() 
        {
            throw new NotImplementedException();
        }
        public virtual void __ForgetIds() 
        {
            throw new NotImplementedException();
        }
        public  DataNode() 
        {
            throw new NotImplementedException();
        }
        public virtual void CompareCheck(ZergRush.Alive.DataNode other, Stack<string> __path) 
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
