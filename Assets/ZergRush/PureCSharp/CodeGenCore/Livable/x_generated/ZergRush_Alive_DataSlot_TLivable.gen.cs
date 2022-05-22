using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class DataSlot<TLivable>
    {
        public enum Types : ushort
        {
        }
        static Func<DataSlot<TLivable>> [] polymorphConstructors = new Func<DataSlot<TLivable>> [] {
        };
        public static DataSlot<TLivable> CreatePolymorphic(System.UInt16 typeId) {
            return polymorphConstructors[typeId]();
        }
    }
}
#endif
