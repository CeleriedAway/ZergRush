using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class Ref<T> : IUpdatableFrom<ZergRush.Alive.Ref<T>>, IUpdatableFrom<ZergRush.Alive.DataNode>, IHashable, ICompareChechable<ZergRush.Alive.DataNode>
    {
        public void UpdateFrom(ZergRush.Alive.Ref<T> other) 
        {
            throw new NotImplementedException();
        }
    }
}
#endif
