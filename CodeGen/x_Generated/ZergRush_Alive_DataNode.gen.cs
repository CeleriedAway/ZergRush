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
            dead = other.dead;
            staticConnections.UpdateFrom(other.staticConnections);
            __parent_id = other.__parent_id;
        }
        public virtual void Deserialize(BinaryReader reader) 
        {
            dead = reader.ReadBoolean();
            staticConnections.Deserialize(reader);
            __parent_id = reader.ReadInt32();
        }
        public virtual void Serialize(BinaryWriter writer) 
        {
            writer.Write(dead);
            staticConnections.Serialize(writer);
            writer.Write(__parent_id);
        }
        public virtual ulong CalculateHash() 
        {
            System.UInt64 hash = 345093625;
            hash += dead ? 1u : 0u;
            hash += hash << 11; hash ^= hash >> 7;
            hash += staticConnections.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)__parent_id;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public virtual void __GenIds(DataRoot __root) 
        {

        }
        public virtual void __PropagateHierarchyAndRememberIds() 
        {

        }
        public virtual void __ForgetIds() 
        {

        }
        public  DataNode() 
        {
            staticConnections = new ZergRush.Alive.StaticConnections();
        }
        public virtual void CompareCheck(ZergRush.Alive.DataNode other, Stack<string> __path) 
        {
            if (dead != other.dead) SerializationTools.LogCompError(__path, "dead", other.dead, dead);
            __path.Push("staticConnections");
            staticConnections.CompareCheck(other.staticConnections, __path);
            __path.Pop();
            if (__parent_id != other.__parent_id) SerializationTools.LogCompError(__path, "__parent_id", other.__parent_id, __parent_id);
        }
        public virtual void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "dead":
                dead = (System.Boolean)reader.Value;
                break;
                case "staticConnections":
                staticConnections.ReadFromJson(reader);
                break;
                case "__parent_id":
                __parent_id = (int)(Int64)reader.Value;
                break;
            }
        }
        public virtual void WriteJsonFields(JsonTextWriter writer) 
        {
            writer.WritePropertyName("dead");
            writer.WriteValue(dead);
            writer.WritePropertyName("staticConnections");
            staticConnections.WriteJson(writer);
            writer.WritePropertyName("__parent_id");
            writer.WriteValue(__parent_id);
        }
    }
}
#endif
