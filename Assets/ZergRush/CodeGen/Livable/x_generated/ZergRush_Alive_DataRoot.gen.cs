using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class DataRoot : IUpdatableFrom<ZergRush.Alive.DataRoot>, IUpdatableFrom<ZergRush.Alive.DataNode>, IHashable, ICompareChechable<ZergRush.Alive.DataNode>, IJsonSerializable
    {
        public void UpdateFrom(ZergRush.Alive.DataRoot other) 
        {
            throw new NotImplementedException();
        }
        public  DataRoot() 
        {
            throw new NotImplementedException();
        }
    }
}
#endif
