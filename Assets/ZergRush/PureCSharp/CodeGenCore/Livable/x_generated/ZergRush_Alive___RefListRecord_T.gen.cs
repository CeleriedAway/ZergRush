using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial struct __RefListRecord<T> : ICompareCheckable<ZergRush.Alive.__RefListRecord<T>>
    {
        public void CompareCheck(ZergRush.Alive.__RefListRecord<T> other, ZRCompareCheckHelper __helper, Action<string> printer) 
        {
            if (id != other.id) CodegenImplTools.LogCompError(__helper, "id", printer, other.id, id);
        }
    }
}
#endif
