using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class GameConfigBaseBase : IHashable, IJsonSerializable, IPolymorphable
    {
        public enum Types : ushort
        {
            GameConfigBaseBase = 1,
            GameConfigExample = 2,
        }
        static Func<GameConfigBaseBase> [] polymorphConstructors = new Func<GameConfigBaseBase> [] {
            () => null, // 0
            () => new ZergRush.Alive.GameConfigBaseBase(), // 1
            () => new ZergRush.Alive.GameConfigExample(), // 2
        };
        public static GameConfigBaseBase CreatePolymorphic(System.UInt16 typeId) {
            return polymorphConstructors[typeId]();
        }
        public virtual void Deserialize(BinaryReader reader) 
        {

        }
        public virtual void Serialize(BinaryWriter writer) 
        {

        }
        public virtual ulong CalculateHash() 
        {
            System.UInt64 hash = 345093625;
            hash += (ulong)1071008758;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public virtual void CollectConfigs(ConfigRegister _collection) 
        {

        }
        public  GameConfigBaseBase() 
        {

        }
        public virtual void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            switch(__name)
            {
            }
        }
        public virtual void WriteJsonFields(JsonTextWriter writer) 
        {

        }
        public virtual ushort GetClassId() 
        {
        return (System.UInt16)Types.GameConfigBaseBase;
        }
        public virtual ZergRush.Alive.GameConfigBaseBase NewInst() 
        {
        return new GameConfigBaseBase();
        }
    }
}
#endif
