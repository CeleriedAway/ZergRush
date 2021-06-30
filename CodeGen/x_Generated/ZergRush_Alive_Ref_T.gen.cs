using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class Ref<T> : IUpdatableFrom<ZergRush.Alive.Ref<T>>, IUpdatableFrom<ZergRush.Alive.DataNode>, IHashable, ICompareChechable<ZergRush.Alive.DataNode>
    {
        public override void UpdateFrom(ZergRush.Alive.DataNode other) 
        {
            base.UpdateFrom(other);
            var otherConcrete = (ZergRush.Alive.Ref<T>)other;
            __id = otherConcrete.__id;
        }
        public void UpdateFrom(ZergRush.Alive.Ref<T> other) 
        {
            this.UpdateFrom((ZergRush.Alive.DataNode)other);
        }
        public override void Deserialize(BinaryReader reader) 
        {
            base.Deserialize(reader);
            __id = reader.ReadInt32();
        }
        public override void Serialize(BinaryWriter writer) 
        {
            base.Serialize(writer);
            writer.Write(__id);
        }
        public override ulong CalculateHash() 
        {
            var baseVal = base.CalculateHash();
            System.UInt64 hash = baseVal;
            hash += (ulong)1940509952;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)__id;
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
        public override void CompareCheck(ZergRush.Alive.DataNode other, Stack<string> __path) 
        {
            base.CompareCheck(other,__path);
            var otherConcrete = (ZergRush.Alive.Ref<T>)other;
            if (__id != otherConcrete.__id) SerializationTools.LogCompError(__path, "__id", otherConcrete.__id, __id);
        }
    }
}
#endif
