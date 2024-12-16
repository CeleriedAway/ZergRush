using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace ZergRush
{
    public class ZRBinaryWriter : BinaryWriter
    {
        ZRObjectIDGenerator generator = new ();

        protected ZRBinaryWriter()
        {
        }

        public ZRBinaryWriter([NotNull] Stream output) : base(output)
        {
        }

        public void Reuse()
        {
            generator.Clear();
            this.Seek(0, SeekOrigin.Begin);
        }

        public void WriteObjectWithRef(IBinarySerializable obj)
        {
            var writer = this;
            // if (obj == null)
            // {
            //     writer.WriteNull();
            //     return;
            // }
            // writer.WriteStartObject();
            // var polymorph = obj as IPolymorphable;
            // if (polymorph != null)
            // {
            //     writer.WritePropertyName("classId");
            //     writer.WriteValue(polymorph.GetClassId());
            // }
    
            var refId = generator.GetId(obj, out bool firstTime);
            writer.Write(!firstTime);
            writer.Write(refId);
    
            if (firstTime)
            {
                obj.Serialize(writer);
            }
        }
    }
    
    public class ZRBinaryReader : BinaryReader
    {
        readonly Dictionary<long, object> currentObjects = new Dictionary<long, object>();

        public ZRBinaryReader(Stream reader) : base(reader)
        {
        }
        public ZRBinaryReader(byte[] str) : base(new MemoryStream(str))
        {
        }
        
        public unsafe ZRBinaryReader(ReadOnlySpan<byte> str) : base(new UnmanagedMemoryStream((byte *)str.GetPinnableReference(), str.Length))
        {
            
        }

        public void ReadFromRef<T>(ref T t) where T : IBinaryDeserializable
        {
            bool isReference = ReadBoolean();
            long refId = ReadInt64();
            if (isReference)
            {
                if (currentObjects.TryGetValue(refId, out object value))
                {
                    t = (T) value;
                }
                else
                {
                    throw new ZergRushException(
                        $"data layout corrupted, can't find reference to {typeof(T)} ref:{refId} in currently processed objects");
                }
            }
            else
            {
                //think about if if it is necessary to do actually
                //if (t == null) t = new T();
                currentObjects[refId] = t;
                t.Deserialize(this);
            }
        }
    }
}