using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class IntListStub : IUpdatableFrom<ZergRush.Alive.IntListStub>, IHashable, ICompareChechable<ZergRush.Alive.IntListStub>
    {
        public virtual void UpdateFrom(ZergRush.Alive.IntListStub other) 
        {
            stub.UpdateFrom(other.stub);
        }
        public virtual ulong CalculateHash() 
        {
            System.UInt64 hash = 345093625;
            hash += (ulong)1909710134;
            hash += hash << 11; hash ^= hash >> 7;
            hash += stub.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public virtual void CompareCheck(ZergRush.Alive.IntListStub other, Stack<string> __path, Action<string> printer) 
        {
            __path.Push("stub");
            stub.CompareCheck(other.stub, __path, printer);
            __path.Pop();
        }
    }
}
#endif
