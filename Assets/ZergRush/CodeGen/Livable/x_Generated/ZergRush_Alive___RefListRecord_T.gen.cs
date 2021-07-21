using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial struct __RefListRecord<T> : ICompareChechable<ZergRush.Alive.__RefListRecord<T>>
    {
        public void CompareCheck(ZergRush.Alive.__RefListRecord<T> other, Stack<string> __path) 
        {
            if (id != other.id) SerializationTools.LogCompError(__path, "id", other.id, id);
        }
    }
}
#endif
