using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Samples {

    public partial class Ancestor : IUpdatableFrom<ZergRush.Samples.Ancestor>, IUpdatableFrom<ZergRush.Samples.CodeGenSamples>, IHashable, ICompareChechable<ZergRush.Samples.CodeGenSamples>, IJsonSerializable, IPolymorphable
    {
        public override void UpdateFrom(ZergRush.Samples.CodeGenSamples other) 
        {
            base.UpdateFrom(other);
            var otherConcrete = (ZergRush.Samples.Ancestor)other;
            fields = otherConcrete.fields;
        }
        public void UpdateFrom(ZergRush.Samples.Ancestor other) 
        {
            this.UpdateFrom((ZergRush.Samples.CodeGenSamples)other);
        }
        public override void Deserialize(BinaryReader reader) 
        {
            base.Deserialize(reader);
            fields = reader.ReadInt32();
        }
        public override void Serialize(BinaryWriter writer) 
        {
            base.Serialize(writer);
            writer.Write(fields);
        }
        public override ulong CalculateHash() 
        {
            var baseVal = base.CalculateHash();
            System.UInt64 hash = baseVal;
            hash += (ulong)1924306019;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)fields;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public  Ancestor() 
        {

        }
        public override void CompareCheck(ZergRush.Samples.CodeGenSamples other, Stack<string> __path) 
        {
            base.CompareCheck(other,__path);
            var otherConcrete = (ZergRush.Samples.Ancestor)other;
            if (fields != otherConcrete.fields) SerializationTools.LogCompError(__path, "fields", otherConcrete.fields, fields);
        }
        public override void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            base.ReadFromJsonField(reader,__name);
            switch(__name)
            {
                case "fields":
                fields = (int)(Int64)reader.Value;
                break;
            }
        }
        public override void WriteJsonFields(JsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);
            writer.WritePropertyName("fields");
            writer.WriteValue(fields);
        }
        public override ushort GetClassId() 
        {
        return (System.UInt16)Types.Ancestor;
        }
        public override ZergRush.Samples.CodeGenSamples NewInst() 
        {
        return new Ancestor();
        }
    }
}
#endif
