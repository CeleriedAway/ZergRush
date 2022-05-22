using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class LivableSlot<TLivable>
    {
        public enum Types : ushort
        {
        }
        static Func<LivableSlot<TLivable>> [] polymorphConstructors = new Func<LivableSlot<TLivable>> [] {
        };
        public static LivableSlot<TLivable> CreatePolymorphic(System.UInt16 typeId) {
            return polymorphConstructors[typeId]();
        }
    }
}
#endif
