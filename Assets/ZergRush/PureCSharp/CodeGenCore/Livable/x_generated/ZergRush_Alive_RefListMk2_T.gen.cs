using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using ZergRush;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class RefListMk2<T> : IBinaryDeserializable, IBinarySerializable, IJsonSerializable
    {
        public void Deserialize(ZRBinaryReader reader) 
        {
            ids.Deserialize(reader);
        }
        public void Serialize(ZRBinaryWriter writer) 
        {
            ids.Serialize(writer);
        }
        public bool ReadFromJsonField(ZRJsonTextReader reader, string __name) 
        {
            switch(__name)
            {
                case "ids":
                ids.ReadFromJson(reader);
                break;
                default: return false; break;
            }
            return true;
        }
        public void WriteJsonFields(ZRJsonTextWriter writer) 
        {
            writer.WritePropertyName("ids");
            ids.WriteJson(writer);
        }
    }
}
#endif
