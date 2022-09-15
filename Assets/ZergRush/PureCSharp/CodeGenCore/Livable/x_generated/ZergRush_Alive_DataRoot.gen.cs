using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class DataRoot : IUpdatableFrom<ZergRush.Alive.DataRoot>, IUpdatableFrom<ZergRush.Alive.DataNode>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareChechable<ZergRush.Alive.DataNode>, IJsonSerializable
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
        public override void CompareCheck(ZergRush.Alive.DataNode other, Stack<string> __path, Action<string> printer) 
        {
            base.CompareCheck(other,__path,printer);
            var otherConcrete = (ZergRush.Alive.DataRoot)other;
            if (__entityIdFactory != otherConcrete.__entityIdFactory) SerializationTools.LogCompError(__path, "__entityIdFactory", printer, otherConcrete.__entityIdFactory, __entityIdFactory);
        }
        public override bool ReadFromJsonField(ZRJsonTextReader reader, string __name) 
        {
            if (base.ReadFromJsonField(reader, __name)) return true;
            switch(__name)
            {
                case "__entityIdFactory":
                __entityIdFactory = (int)(Int64)reader.Value;
                break;
                default: return false; break;
            }
            return true;
        }
        public override void WriteJsonFields(ZRJsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);
            writer.WritePropertyName("__entityIdFactory");
            writer.WriteValue(__entityIdFactory);
        }
    }
}
#endif
