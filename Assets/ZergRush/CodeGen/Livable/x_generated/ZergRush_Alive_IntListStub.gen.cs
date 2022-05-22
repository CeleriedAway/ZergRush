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
            throw new NotImplementedException();
        }
        public virtual ulong CalculateHash() 
        {
            throw new NotImplementedException();
        }
        public virtual void CompareCheck(ZergRush.Alive.IntListStub other, Stack<string> __path) 
        {
            throw new NotImplementedException();
        }

        public void CompareCheck(IntListStub t, Stack<string> path, Action<string> printer)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
