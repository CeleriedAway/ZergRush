using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class DataRoot : IUpdatableFrom<ZergRush.Alive.DataRoot>, IUpdatableFrom<ZergRush.Alive.DataNode>, IHashable, ICompareChechable<ZergRush.Alive.DataNode>, IJsonSerializable
    {
        public override void UpdateFrom(ZergRush.Alive.DataNode other) 
        {
            base.UpdateFrom(other);
            var otherConcrete = (ZergRush.Alive.DataRoot)other;
            __entityIdFactory = otherConcrete.__entityIdFactory;
        }
        public void UpdateFrom(ZergRush.Alive.DataRoot other) 
        {
            this.UpdateFrom((ZergRush.Alive.DataNode)other);
        }
        public override void Deserialize(BinaryReader reader) 
        {
            base.Deserialize(reader);
            __entityIdFactory = reader.ReadInt32();
        }
        public override void Serialize(BinaryWriter writer) 
        {
            base.Serialize(writer);
            writer.Write(__entityIdFactory);
        }
        public override ulong CalculateHash() 
        {
            var baseVal = base.CalculateHash();
            System.UInt64 hash = baseVal;
            hash += (System.UInt64)__entityIdFactory;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public override void __GenIds(DataRoot __root) 
        {
            base.__GenIds(__root);

        }
        public override void __PropagateHierarchyAndRememberIds() 
        {
            base.__PropagateHierarchyAndRememberIds();

        }
        public override void __ForgetIds() 
        {
            base.__ForgetIds();

        }
        public  DataRoot() 
        {

        }
        public override void CompareCheck(ZergRush.Alive.DataNode other, Stack<string> __path) 
        {
            base.CompareCheck(other,__path);
            var otherConcrete = (ZergRush.Alive.DataRoot)other;
            if (__entityIdFactory != otherConcrete.__entityIdFactory) SerializationTools.LogCompError(__path, "__entityIdFactory", otherConcrete.__entityIdFactory, __entityIdFactory);
        }
        public override void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            base.ReadFromJsonField(reader,__name);
            switch(__name)
            {
                case "__entityIdFactory":
                __entityIdFactory = (int)(Int64)reader.Value;
                break;
            }
        }
        public override void WriteJsonFields(JsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);
            writer.WritePropertyName("__entityIdFactory");
            writer.WriteValue(__entityIdFactory);
        }
    }
}
#endif
