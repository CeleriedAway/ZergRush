using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class LivableRoot : IUpdatableFrom<ZergRush.Alive.LivableRoot>, IUpdatableFrom<ZergRush.Alive.DataNode>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareChechable<ZergRush.Alive.DataNode>, IJsonSerializable
    {
        public override void UpdateFrom(ZergRush.Alive.DataNode other) 
        {
            base.UpdateFrom(other);
            var otherConcrete = (ZergRush.Alive.LivableRoot)other;
        }
        public void UpdateFrom(ZergRush.Alive.LivableRoot other) 
        {
            this.UpdateFrom((ZergRush.Alive.DataNode)other);
        }
        public override void Deserialize(BinaryReader reader) 
        {
            base.Deserialize(reader);

        }
        public override void Serialize(BinaryWriter writer) 
        {
            base.Serialize(writer);

        }
        public override ulong CalculateHash() 
        {
            var baseVal = base.CalculateHash();
            System.UInt64 hash = baseVal;
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
        public  LivableRoot() 
        {

        }
        public override bool ReadFromJsonField(ZRJsonTextReader reader, string __name) 
        {
            if (base.ReadFromJsonField(reader, __name)) return true;
            switch(__name)
            {
                default: return false; break;
            }
            return true;
        }
        public override void WriteJsonFields(ZRJsonTextWriter writer) 
        {
            base.WriteJsonFields(writer);

        }
    }
}
#endif
