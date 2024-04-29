using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Samples {

    public partial class Ancestor : IUpdatableFrom<ZergRush.Samples.Ancestor>, IUpdatableFrom<ZergRush.Samples.CodeGenSamples>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareChechable<ZergRush.Samples.CodeGenSamples>, IJsonSerializable, IPolymorphable, ICloneInst
    {
        public override void UpdateFrom(ZergRush.Samples.CodeGenSamples other, ZRUpdateFromHelper __helper) 
        {
            base.UpdateFrom(other,__helper);
            var otherConcrete = (ZergRush.Samples.Ancestor)other;
            fields = otherConcrete.fields;
        }
        public void UpdateFrom(ZergRush.Samples.Ancestor other, ZRUpdateFromHelper __helper) 
        {
            this.UpdateFrom((ZergRush.Samples.CodeGenSamples)other, __helper);
        }
        public override void Deserialize(ZRBinaryReader reader) 
        {
            base.Deserialize(reader);
            fields = reader.ReadInt32();
        }
        public override void Serialize(ZRBinaryWriter writer) 
        {
            base.Serialize(writer);
            writer.Write(fields);
        }
        public override ulong CalculateHash(ZRHashHelper __helper) 
        {
            var baseVal = base.CalculateHash(__helper);
            System.UInt64 hash = baseVal;
            hash ^= (ulong)1835811306;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)fields;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public  Ancestor() 
        {

        }
        public override void CompareCheck(ZergRush.Samples.CodeGenSamples other, ZRCompareCheckHelper __helper, Action<string> printer) 
        {
            base.CompareCheck(other,__helper,printer);
            var otherConcrete = (ZergRush.Samples.Ancestor)other;
            if (fields != otherConcrete.fields) SerializationTools.LogCompError(__helper, "fields", printer, otherConcrete.fields, fields);
        }
        public override bool ReadFromJsonField(ZRJsonTextReader reader, string __name) 
        {
            if (base.ReadFromJsonField(reader, __name)) return true;
            switch(__name)
            {
                case "fields":
                fields = (int)(Int64)reader.Value;
                break;
                default: return false; break;
            }
            return true;
        }
        public override void WriteJsonFields(ZRJsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);
            writer.WritePropertyName("fields");
            writer.WriteValue(fields);
        }
        public override ushort GetClassId() 
        {
        return (System.UInt16)Types.Ancestor;
        }
        public override System.Object NewInst() 
        {
        return new Ancestor();
        }
    }
}
#endif
