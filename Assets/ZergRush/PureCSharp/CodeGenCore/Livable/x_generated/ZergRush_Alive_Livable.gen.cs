using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class Livable : IUpdatableFrom<ZergRush.Alive.Livable>, IUpdatableFrom<ZergRush.Alive.DataNode>, IBinaryDeserializable, IBinarySerializable, IHashable, ICompareCheckable<ZergRush.Alive.DataNode>, IJsonSerializable
    {
        public override void UpdateFrom(ZergRush.Alive.DataNode other, ZRUpdateFromHelper __helper) 
        {
            base.UpdateFrom(other,__helper);
            var otherConcrete = (ZergRush.Alive.Livable)other;
        }
        public void UpdateFrom(ZergRush.Alive.Livable other, ZRUpdateFromHelper __helper) 
        {
            this.UpdateFrom((ZergRush.Alive.DataNode)other, __helper);
        }
        public override void Deserialize(ZRBinaryReader reader) 
        {
            base.Deserialize(reader);

        }
        public override void Serialize(ZRBinaryWriter writer) 
        {
            base.Serialize(writer);

        }
        public override ulong CalculateHash(ZRHashHelper __helper) 
        {
            var baseVal = base.CalculateHash(__helper);
            System.UInt64 hash = baseVal;
            return hash;
        }
        public virtual void Enlive() 
        {
            EnliveSelf();
            EnliveChildren();
        }
        public virtual void Mortify() 
        {
            MortifySelf();
            MortifyChildren();
        }
        protected virtual void EnliveChildren() 
        {

        }
        protected virtual void MortifyChildren() 
        {

        }
        public override void VisitNode(Action<object> action) 
        {
            base.VisitNode(action);

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
        public  Livable() 
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
