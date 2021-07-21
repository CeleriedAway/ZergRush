using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Samples {

    public partial class CodeGenSamples : IUpdatableFrom<ZergRush.Samples.CodeGenSamples>, IHashable, ICompareChechable<ZergRush.Samples.CodeGenSamples>, IJsonSerializable, IPolymorphable
    {
        public enum Types : ushort
        {
            CodeGenSamples = 1,
            Ancestor = 2,
        }
        static Func<CodeGenSamples> [] polymorphConstructors = new Func<CodeGenSamples> [] {
        };
        public static CodeGenSamples CreatePolymorphic(System.UInt16 typeId) {
            return polymorphConstructors[typeId]();
        }
        public virtual void UpdateFrom(ZergRush.Samples.CodeGenSamples other) 
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
        public  CodeGenSamples() 
        {
            throw new NotImplementedException();
        }
        public virtual void CompareCheck(ZergRush.Samples.CodeGenSamples other, Stack<string> __path) 
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
        public virtual ushort GetClassId() 
        {
            throw new NotImplementedException();
        }
        public virtual ZergRush.Samples.CodeGenSamples NewInst() 
        {
            throw new NotImplementedException();
        }
    }
}
#endif
