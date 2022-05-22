using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class LivableRoot : IUpdatableFrom<ZergRush.Alive.LivableRoot>, IUpdatableFrom<ZergRush.Alive.DataNode>, IHashable, ICompareChechable<ZergRush.Alive.DataNode>, IJsonSerializable
    {
        public void UpdateFrom(ZergRush.Alive.LivableRoot other) 
        {
            throw new NotImplementedException();
        }
        public  LivableRoot() 
        {
            throw new NotImplementedException();
        }

        public void UpdateFrom(DataNode val)
        {
            throw new NotImplementedException();
        }

        public ulong CalculateHash()
        {
            throw new NotImplementedException();
        }

        public void CompareCheck(DataNode t, Stack<string> path, Action<string> printer)
        {
            throw new NotImplementedException();
        }

        public void WriteJsonFields(JsonTextWriter writer)
        {
            throw new NotImplementedException();
        }

        public void ReadFromJsonField(JsonTextReader reader, string name)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
