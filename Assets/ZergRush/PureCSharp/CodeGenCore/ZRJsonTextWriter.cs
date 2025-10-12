using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using ZergRush;

namespace ZergRush
{
    public class ZRJsonTextWriter : JsonTextWriter
    {
        ObjectIDGenerator generator = new ObjectIDGenerator();

        public ZRJsonTextWriter(TextWriter textWriter) : base(textWriter)
        {
        }
        
        public void RegisterFirstObject(IJsonSerializable obj)
        {
            generator.GetId(obj, out bool firstTime);
            if (!firstTime)
            {
                throw new ZergRushException("Object was already registered");
            }
        }

        public void WriteObjectWithRef(IJsonSerializable obj)
        {
            var writer = this;
            if (obj == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            var polymorph = obj as IPolymorphable;
            if (polymorph != null)
            {
                writer.WritePropertyName(CodeGenImplTools.ClassIdName);
                writer.WriteValue(polymorph.GetClassId());
            }

            var refId = generator.GetId(obj, out bool firstTime);
            writer.WritePropertyName("isRef");
            writer.WriteValue(!firstTime);

            writer.WritePropertyName("refId");
            writer.WriteValue(refId.ToString());

            if (firstTime)
            {
                obj.WriteJsonFields(writer);
            }
            writer.WriteEndObject();
        }
    }
}