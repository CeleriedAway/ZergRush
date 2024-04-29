using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Samples {

    public partial class OtherData : IUpdatableFrom<ZergRush.Samples.OtherData>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareChechable<ZergRush.Samples.OtherData>, IJsonSerializable
    {
        public virtual void UpdateFrom(ZergRush.Samples.OtherData other, ZRUpdateFromHelper __helper) 
        {
            someData = other.someData;
        }
        public virtual void Deserialize(ZRBinaryReader reader) 
        {
            someData = reader.ReadInt32();
        }
        public virtual void Serialize(ZRBinaryWriter writer) 
        {
            writer.Write(someData);
        }
        public virtual ulong CalculateHash(ZRHashHelper __helper) 
        {
            System.UInt64 hash = 345093625;
            hash ^= (ulong)743171211;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)someData;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public virtual void CompareCheck(ZergRush.Samples.OtherData other, ZRCompareCheckHelper __helper, Action<string> printer) 
        {
            if (someData != other.someData) SerializationTools.LogCompError(__helper, "someData", printer, other.someData, someData);
        }
        public virtual bool ReadFromJsonField(ZRJsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "someData":
                someData = (int)(Int64)reader.Value;
                break;
                default: return false; break;
            }
            return true;
        }
        public virtual void WriteJsonFields(ZRJsonTextWriter writer) 
        {
            writer.WritePropertyName("someData");
            writer.WriteValue(someData);
        }
        public  OtherData() 
        {

        }
    }
}
#endif
