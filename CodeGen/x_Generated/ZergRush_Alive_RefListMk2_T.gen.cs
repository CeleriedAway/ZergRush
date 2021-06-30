using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class RefListMk2<T> : IJsonSerializable
    {
        public void Deserialize(BinaryReader reader) 
        {
            ids.Deserialize(reader);
        }
        public void Serialize(BinaryWriter writer) 
        {
            ids.Serialize(writer);
        }
        public void ReadFromJsonField(JsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "ids":
                ids.ReadFromJson(reader);
                break;
            }
        }
        public void WriteJsonFields(JsonTextWriter writer) 
        {
            writer.WritePropertyName("ids");
            ids.WriteJson(writer);
        }
    }
}
#endif
