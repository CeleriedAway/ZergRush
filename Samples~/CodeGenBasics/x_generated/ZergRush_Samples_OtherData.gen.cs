using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Samples {

    public partial class OtherData : IUpdatableFrom<ZergRush.Samples.OtherData>, IHashable, ICompareChechable<ZergRush.Samples.OtherData>, IJsonSerializable
    {
        public virtual void UpdateFrom(ZergRush.Samples.OtherData other) 
        {
            someData = other.someData;
        }
        public virtual void Deserialize(BinaryReader reader) 
        {
            someData = reader.ReadInt32();
        }
        public virtual void Serialize(BinaryWriter writer) 
        {
            writer.Write(someData);
        }
        public virtual ulong CalculateHash() 
        {
            System.UInt64 hash = 345093625;
            hash += (ulong)896074244;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)someData;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public  OtherData() 
        {

        }
        public virtual void CompareCheck(ZergRush.Samples.OtherData other, Stack<string> __path) 
        {
            if (someData != other.someData) SerializationTools.LogCompError(__path, "someData", other.someData, someData);
        }
        public virtual void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "someData":
                someData = (int)(Int64)reader.Value;
                break;
            }
        }
        public virtual void WriteJsonFields(JsonTextWriter writer) 
        {
            writer.WritePropertyName("someData");
            writer.WriteValue(someData);
        }
    }
}
#endif
