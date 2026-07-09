using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush {

    public partial class ZergRandom : IUpdatableFrom<ZergRush.ZergRandom>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareCheckable<ZergRush.ZergRandom>, IJsonSerializable
    {
        public virtual void UpdateFrom(ZergRush.ZergRandom other, ZRUpdateFromHelper __helper) 
        {
            inext = other.inext;
            inextp = other.inextp;
            var SeedArrayCount = other.SeedArray.Length;
            var SeedArrayTemp = SeedArray;
            Array.Resize(ref SeedArrayTemp, SeedArrayCount);
            SeedArray = SeedArrayTemp;
            SeedArray.UpdateFrom(other.SeedArray, __helper);
        }
        public virtual void Deserialize(ZRBinaryReader reader) 
        {
            inext = reader.ReadInt32();
            inextp = reader.ReadInt32();
            SeedArray = reader.ReadSystem_Int32_Array();
        }
        public virtual void Serialize(ZRBinaryWriter writer) 
        {
            writer.Write(inext);
            writer.Write(inextp);
            SeedArray.Serialize(writer);
        }
        public virtual ulong CalculateHash(ZRHashHelper __helper) 
        {
            System.UInt64 hash = 345093625;
            hash ^= (ulong)502686759;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)inext;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)inextp;
            hash += hash << 11; hash ^= hash >> 7;
            hash += SeedArray.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public virtual void CompareCheck(ZergRush.ZergRandom other, ZRCompareCheckHelper __helper, Action<string> printer) 
        {
            if (inext != other.inext) CodeGenImplTools.LogCompError(__helper, "inext", printer, other.inext, inext);
            if (inextp != other.inextp) CodeGenImplTools.LogCompError(__helper, "inextp", printer, other.inextp, inextp);
            __helper.Push("SeedArray");
            SeedArray.CompareCheck(other.SeedArray, __helper, printer);
            __helper.Pop();
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
