using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class GameLoadableConfigExample : IHashable, IUniquelyIdentifiable, IJsonSerializable, IPolymorphable
    {
        public enum Types : ushort
        {
            GameLoadableConfigExample = 1,
        }
        static Func<GameLoadableConfigExample> [] polymorphConstructors = new Func<GameLoadableConfigExample> [] {
            () => null, // 0
            () => new ZergRush.Alive.GameLoadableConfigExample(), // 1
        };
        public static GameLoadableConfigExample CreatePolymorphic(System.UInt16 typeId) {
            return polymorphConstructors[typeId]();
        }
        public override void Deserialize(BinaryReader reader) 
        {
            base.Deserialize(reader);
            uid = reader.ReadString();
        }
        public override void Serialize(BinaryWriter writer) 
        {
            base.Serialize(writer);
            writer.Write(uid);
        }
        public override ulong CalculateHash() 
        {
            var baseVal = base.CalculateHash();
            System.UInt64 hash = baseVal;
            hash += (ulong)1159801892;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (ulong)uid.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public override ulong UId() 
        {
            System.UInt64 hash = GetClassId();
            hash += (ulong)uid.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public override void CollectConfigs(ConfigRegister _collection) 
        {
            base.CollectConfigs(_collection);

        }
        public  GameLoadableConfigExample() 
        {
            uid = string.Empty;
        }
        public override void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            base.ReadFromJsonField(reader,__name);
            switch(__name)
            {
                case "uid":
                uid = (string) reader.Value;
                break;
            }
        }
        public override void WriteJsonFields(JsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);
            writer.WritePropertyName("uid");
            writer.WriteValue(uid);
        }
        public virtual ushort GetClassId() 
        {
        return (System.UInt16)Types.GameLoadableConfigExample;
        }
        public virtual ZergRush.Alive.GameLoadableConfigExample NewInst() 
        {
        return new GameLoadableConfigExample();
        }
    }
}
#endif
