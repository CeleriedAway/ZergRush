using System;
using System.Collections.Generic;
using ZergRush.CodeGen;

namespace ZergRush.Alive
{
    [GenTask((GenTaskFlags.ConfigData | GenTaskFlags.UIDGen) & ~GenTaskFlags.PolymorphicConstruction), GenInLocalFolder, Immutable, GenTaskCustomImpl(GenTaskFlags.UIDGen)]
    public partial class LoadableConfig : IUniquelyIdentifiable
    {
        public ulong id => UId();
        public virtual ulong UId() => 0;
    }
    
    [GenTask((GenTaskFlags.ConfigData | GenTaskFlags.UIDGen) & ~GenTaskFlags.PolymorphicConstruction), GenInLocalFolder]
    public partial class StubTypeBasedDataFromConfig : LoadableConfig
    {        
    }
    
    public class ConfigRegister : Dictionary<ulong, IUniquelyIdentifiable> {}
    
    // list that force direct serialization of its members instead of reading from config
    public class ConfigStorageList<T> : List<T> {}
    public class ConfigStorageDict<TKey, T> : Dictionary<TKey, T> {}
    
    //[GenHashing]
}
