using Newtonsoft.Json;
using ZergRush;

public interface IStableIdentifiable 
{
    public int stableId { get; }
}

public static partial class SerializationTools
{
    // Return if needs to check further

    public static uint CalculateHash(this byte[] array, ZRHashHelper _)
    {
        uint hash = 0;
        for (int i = 0; i < array.Length; i++)
        {
            hash += array[i];
            hash += hash << 10;
            hash ^= hash >> 6;
        }
        return hash;
    }

    public static void ReadSkipComments(this JsonTextReader reader)
    {
        while (reader.Read() && reader.TokenType == JsonToken.Comment)
        {
        }
    }

    public static void WriteJson(this IJsonSerializable obj, ZRJsonTextWriter writer)
    {
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

        obj.WriteJsonFields(writer);
        writer.WriteEndObject();
    }

    public static T ReadAsJsonRoot<T>(this ZRJsonTextReader reader, T obj = null)
        where T : class, IJsonSerializable, new()
    {
        if (obj == null) obj = new T();
        reader.Read();
        obj.ReadFromJson(reader);
        return obj;
    }
}