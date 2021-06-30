using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.ReactiveCore {

    public partial struct LEvent : IUpdatableFrom<ZergRush.ReactiveCore.LEvent>, IHashable, ICompareChechable<ZergRush.ReactiveCore.LEvent>
    {
        public void UpdateFrom(ZergRush.ReactiveCore.LEvent other) 
        {

        }
        public void Deserialize(BinaryReader reader) 
        {

        }
        public void Serialize(BinaryWriter writer) 
        {

        }
        public ulong CalculateHash() 
        {
            System.UInt64 hash = 345093625;
            hash += (ulong)1980033062;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public void CompareCheck(ZergRush.ReactiveCore.LEvent other, Stack<string> __path) 
        {

        }
        public void ReadFromJson(JsonTextReader reader) 
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var __name = (string) reader.Value;
                    reader.Read();
                    switch(__name)
                    {
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject) { break; }
            }
        }
        public void WriteJson(JsonTextWriter writer) 
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
#endif
