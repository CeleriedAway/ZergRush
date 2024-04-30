using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class Ref<T> : IUpdatableFrom<ZergRush.Alive.Ref<T>>, IUpdatableFrom<ZergRush.Alive.DataNode>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareCheckable<ZergRush.Alive.DataNode>
    {
        public override void UpdateFrom(ZergRush.Alive.DataNode other, ZRUpdateFromHelper __helper) 
        {
            base.UpdateFrom(other,__helper);
            var otherConcrete = (ZergRush.Alive.Ref<T>)other;
            __id = otherConcrete.__id;
        }
        public void UpdateFrom(ZergRush.Alive.Ref<T> other, ZRUpdateFromHelper __helper) 
        {
            this.UpdateFrom((ZergRush.Alive.DataNode)other, __helper);
        }
        public override void Deserialize(ZRBinaryReader reader) 
        {
            base.Deserialize(reader);
            __id = reader.ReadInt32();
        }
        public override void Serialize(ZRBinaryWriter writer) 
        {
            base.Serialize(writer);
            writer.Write(__id);
        }
        public override ulong CalculateHash(ZRHashHelper __helper) 
        {
            var baseVal = base.CalculateHash(__helper);
            System.UInt64 hash = baseVal;
            hash ^= (ulong)1374988313;
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
        public override void CompareCheck(ZergRush.Alive.DataNode other, ZRCompareCheckHelper __helper, Action<string> printer) 
        {
            base.CompareCheck(other,__helper,printer);
            var otherConcrete = (ZergRush.Alive.Ref<T>)other;
            if (__id != otherConcrete.__id) CodeGenImplTools.LogCompError(__helper, "__id", printer, otherConcrete.__id, __id);
        }
    }
}
#endif
