using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ZergRush
{
    public class ZRHashHelper
    {
        ObjectIDGenerator generator = new ObjectIDGenerator();
        Dictionary<long, ulong> alreadyHashed = new();
        
        public ulong CalculateHash<T>(T source) where T : IHashable
        {
            var id = generator.GetId(source, out var firstTime);
            if (firstTime)
            {
                var hash = source.CalculateHash(this);
                alreadyHashed[id] = hash;
                return hash ^ 0x2345096349;
            }
            else
            {
                return alreadyHashed[id];
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