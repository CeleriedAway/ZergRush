using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class DataNode : IUpdatableFrom<ZergRush.Alive.DataNode>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareChechable<ZergRush.Alive.DataNode>, IJsonSerializable
    {
        public virtual void UpdateFrom(ZergRush.Alive.DataNode other, ZRUpdateFromHelper __helper) 
        {
            dead = other.dead;
            staticConnections.UpdateFrom(other.staticConnections, __helper);
        }
        public virtual void Deserialize(BinaryReader reader) 
        {
            dead = reader.ReadBoolean();
            staticConnections.Deserialize(reader);
        }
        public virtual void Serialize(BinaryWriter writer) 
        {
            writer.Write(dead);
            staticConnections.Serialize(writer);
        }
        public virtual ulong CalculateHash(ZRHashHelper __helper) 
        {
            System.UInt64 hash = 345093625;
            hash += dead ? 1u : 0u;
            hash += hash << 11; hash ^= hash >> 7;
            hash += staticConnections.CalculateHash(__helper);
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
        public virtual void CompareCheck(ZergRush.Alive.DataNode other, ZRCompareCheckHelper __helper, Action<string> printer) 
        {
            if (dead != other.dead) SerializationTools.LogCompError(__helper, "dead", printer, other.dead, dead);
            __helper.Push("staticConnections");
            staticConnections.CompareCheck(other.staticConnections, __helper, printer);
            __helper.Pop();
        }
        public virtual bool ReadFromJsonField(ZRJsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "dead":
                dead = (bool)reader.Value;
                break;
                case "staticConnections":
                staticConnections.ReadFromJson(reader);
                break;
                default: return false; break;
            }
            return true;
        }
        public virtual void WriteJsonFields(ZRJsonTextWriter writer) 
        {
            writer.WritePropertyName("dead");
            writer.WriteValue(dead);
            writer.WritePropertyName("staticConnections");
            staticConnections.WriteJson(writer);
        }
    }
}
#endif
