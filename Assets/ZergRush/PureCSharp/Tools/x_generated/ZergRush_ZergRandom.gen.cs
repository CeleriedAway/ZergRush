using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush {

    public partial class ZergRandom : IUpdatableFrom<ZergRush.ZergRandom>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareChechable<ZergRush.ZergRandom>, IJsonSerializable
    {
        public virtual void UpdateFrom(ZergRush.ZergRandom other) 
        {
            inext = other.inext;
            inextp = other.inextp;
            var SeedArrayCount = other.SeedArray.Length;
            var SeedArrayTemp = SeedArray;
            Array.Resize(ref SeedArrayTemp, SeedArrayCount);
            SeedArray = SeedArrayTemp;
            SeedArray.UpdateFrom(other.SeedArray);
        }
        public virtual void Deserialize(BinaryReader reader) 
        {
            inext = reader.ReadInt32();
            inextp = reader.ReadInt32();
            SeedArray = reader.ReadSystem_Int32_Array();
        }
        public virtual void Serialize(BinaryWriter writer) 
        {
            writer.Write(inext);
            writer.Write(inextp);
            SeedArray.Serialize(writer);
        }
        public virtual ulong CalculateHash() 
        {
            System.UInt64 hash = 345093625;
            hash += (ulong)502686759;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)inext;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)inextp;
            hash += hash << 11; hash ^= hash >> 7;
            hash += SeedArray.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public virtual void CompareCheck(ZergRush.ZergRandom other, Stack<string> __path, Action<string> printer) 
        {
            if (inext != other.inext) SerializationTools.LogCompError(__path, "inext", printer, other.inext, inext);
            if (inextp != other.inextp) SerializationTools.LogCompError(__path, "inextp", printer, other.inextp, inextp);
            __path.Push("SeedArray");
            SeedArray.CompareCheck(other.SeedArray, __path, printer);
            __path.Pop();
        }
        public virtual bool ReadFromJsonField(ZRJsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "inext":
                inext = (int)(Int64)reader.Value;
                break;
                case "inextp":
                inextp = (int)(Int64)reader.Value;
                break;
                case "SeedArray":
                SeedArray = SeedArray.ReadFromJson(reader);
                break;
                default: return false; break;
            }
            return true;
        }
        public virtual void WriteJsonFields(ZRJsonTextWriter writer) 
        {
            writer.WritePropertyName("inext");
            writer.WriteValue(inext);
            writer.WritePropertyName("inextp");
            writer.WriteValue(inextp);
            writer.WritePropertyName("SeedArray");
            SeedArray.WriteJson(writer);
        }
    }
}
#endif
