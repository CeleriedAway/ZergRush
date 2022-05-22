using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class GameConfigExample : IHashable, IJsonSerializable
    {
        public  GameConfigExample() 
        {
            throw new NotImplementedException();
        }

        public ulong CalculateHash()
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
