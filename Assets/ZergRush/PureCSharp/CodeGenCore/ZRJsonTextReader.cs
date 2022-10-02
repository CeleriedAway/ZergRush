using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ZergRush
{
    public class ZRUpdateFromHelper
    {
        ObjectIDGenerator generator = new ObjectIDGenerator();
        Dictionary<long, object> alreadyUpdated = new Dictionary<long, object>();

        public bool TryLoadAlreadyUpdated<T>(T source, ref T target)
        {
            var id = generator.GetId(source, out var firstTime);
            if (firstTime)
            {
                alreadyUpdated[id] = target;
                return false;
            }
            else
            {
                target = (T)alreadyUpdated[id];
                return true;
            }
        }
    }
    
    public class ZRJsonTextReader : JsonTextReader
    {
        readonly Dictionary<long, object> currentObjects = new Dictionary<long, object>();

        public ZRJsonTextReader(TextReader reader) : base(reader)
        {
        }

        public void ReadFromRef<T>(ref T t) where T : IJsonSerializable
        {
            bool isReference = ReadIsRef(this);
            long refId = ReadRef(this);
            if (isReference)
            {
                if (currentObjects.TryGetValue(refId, out object value))
                {
                    t = (T) value;
                    while (TokenType != JsonToken.EndObject) Read();
                }
                else
                {
                    throw new ZergRushException(
                        $"data layout corrupted, can't find reference to {typeof(T)} ref:{refId} in currently processed objects");
                }
            }
            else
            {
                currentObjects[refId] = t;
                t.ReadFromJson(this);
            }
        }

        static long ReadRef(JsonTextReader reader)
        {
            reader.Read();
            if (reader.TokenType == JsonToken.PropertyName && (string) reader.Value == "refId")
            {
                return long.Parse(reader.ReadAsString());
            }
            else
            {
                throw new ZergRushException("error while reading is ref in json");
            }
        }

        static bool ReadIsRef(JsonTextReader reader)
        {
            reader.Read();
            if (reader.TokenType == JsonToken.PropertyName && (string) reader.Value == "isRef")
            {
                return (bool) reader.ReadAsBoolean();
            }
            else
            {
                throw new ZergRushException("error while reading is ref in json");
            }
        }
    }
}