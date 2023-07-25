using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class RefListFlawless<T> : IUpdatableFrom<ZergRush.Alive.RefListFlawless<T>>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareChechable<ZergRush.Alive.RefListFlawless<T>>, IJsonSerializable
    {
        public void UpdateFrom(ZergRush.Alive.RefListFlawless<T> other, ZRUpdateFromHelper __helper) 
        {
            ids.UpdateFrom(other.ids, __helper);
        }
        public void Deserialize(BinaryReader reader) 
        {
            ids.Deserialize(reader);
        }
        public void Serialize(BinaryWriter writer) 
        {
            ids.Serialize(writer);
        }
        public ulong CalculateHash(ZRHashHelper __helper) 
        {
            System.UInt64 hash = 345093625;
            hash += (ulong)1091248618;
            hash += hash << 11; hash ^= hash >> 7;
            hash += ids.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public void CompareCheck(ZergRush.Alive.RefListFlawless<T> other, ZRCompareCheckHelper __helper, Action<string> printer) 
        {
            __helper.Push("ids");
            ids.CompareCheck(other.ids, __helper, printer);
            __helper.Pop();
        }
        public bool ReadFromJsonField(ZRJsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "ids":
                ids.ReadFromJson(reader);
                break;
                default: return false; break;
            }
            return true;
        }
        public void WriteJsonFields(ZRJsonTextWriter writer) 
        {
            writer.WritePropertyName("ids");
            ids.WriteJson(writer);
        }
    }
}
#endif
