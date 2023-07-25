using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class IntListStub : IUpdatableFrom<ZergRush.Alive.IntListStub>, IHashable, ICompareChechable<ZergRush.Alive.IntListStub>
    {
        public virtual void UpdateFrom(ZergRush.Alive.IntListStub other, ZRUpdateFromHelper __helper) 
        {
            // stub.UpdateFrom(other.stub, __helper);
        }
        public virtual ulong CalculateHash(ZRHashHelper __helper) 
        {
            System.UInt64 hash = 345093625;
            hash += (ulong)1909710134;
            hash += hash << 11; hash ^= hash >> 7;
            // hash += stub.CalculateHash(__helper);
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public virtual void CompareCheck(ZergRush.Alive.IntListStub other, ZRCompareCheckHelper __helper, Action<string> printer) 
        {
            __helper.Push("stub");
            // stub.CompareCheck(other.stub, __helper, printer);
            __helper.Pop();
        }
    }
}
#endif
