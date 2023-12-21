using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ZergRush;
using ZergRush.Alive;
using ZergRush.CodeGen;

public interface IStableIdentifiable 
{
    public int stableId { get; }
}

public static partial class SerializationTools
{
    public static string ClassIdName = "__classId";
    
    public static ulong CalculateHash(this IHashable t)
    {
        return t.CalculateHash(new ZRHashHelper());
    }
    
    public static void CompareCheck<T>(this T t, T other) where T : ICompareChechable<T>
    {
        t.CompareCheck(other, new ZRCompareCheckHelper(), LogSink.errLog);
    }
    
    
    public static void StableUpdateFrom<T>(this IList<T> self, IList<T> other, ZRUpdateFromHelper __helper) 
        where T : IStableIdentifiable, IUpdatableFrom<T>
    {
        var i = 0;
        for (; i < other.Count; i++)
        {
            var currOtherItem = other[i];
            var selfMatchingItemIndex = self.IndexOf(o => o.stableId == currOtherItem.stableId);
            
            check:
            if (selfMatchingItemIndex == i)
            {
                // self is here, just update
                var selfItem = self[i];
                if (selfItem is not IsMultiRef || !__helper.TryLoadAlreadyUpdated(currOtherItem, ref selfItem))
                {
                    selfItem.UpdateFrom(currOtherItem, __helper);
                }
                self[i] = selfItem;
            }
            else if (selfMatchingItemIndex > i)
            {
                var currSelfItem = self[i];
                // check if current self item is absent in other to make the most painless shift
                var otherPosOfCurrentSelf = other.IndexOf(o => o.stableId == currSelfItem.stableId);
                if (otherPosOfCurrentSelf == -1)
                {
                    // this self is redundant, remove, shift index and go again
                    self.RemoveAt(i);
                    selfMatchingItemIndex--;
                    goto check;
                }
                // move item to right position
                var selfItem = self.TakeAt(selfMatchingItemIndex);
                if (selfItem is not IsMultiRef || !__helper.TryLoadAlreadyUpdated(currOtherItem, ref selfItem))
                {
                    selfItem.UpdateFrom(currOtherItem, __helper);
                }
                self.Insert(i, selfItem);
            }
            else if (selfMatchingItemIndex == -1)
            {
                var selfItem = (T) ((ICloneInst)currOtherItem).NewInst();
                // self not found, creating new one
                if (self is IAddCopyList<T> addCopyList)
                {
                    addCopyList.InsertCopy(selfItem, currOtherItem, __helper, i);
                }
                else
                {
                    if (selfItem is not IsMultiRef || !__helper.TryLoadAlreadyUpdated(currOtherItem, ref selfItem))
                    {
                        selfItem.UpdateFrom(currOtherItem, __helper);
                    }
                    self.Insert(i, selfItem);
                }
            }
            else
            {
                throw new ZergRushException("found self in the past, thi should not happer");
            }
        }

        var toRemove = self.Count - i;
        // remove redundant self items if any
        for (int j = 0; j < toRemove; j++)
        {
            self.RemoveAt(self.Count - 1);
        }
    }
    
    public static void UpdateFrom<T>(this T t, T other) where T : IUpdatableFrom<T>
    {
        t.UpdateFrom(other, new ZRUpdateFromHelper());
    }
    
    public static void SkipObj(this JsonTextReader reader)
    {
        if (reader.TokenType == JsonToken.StartObject)
        {
            int objCount = 1;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject) objCount++;
                else if (reader.TokenType == JsonToken.EndObject) objCount--;
                if (objCount == 0) break;
            }
        }
        else if (reader.TokenType == JsonToken.StartArray)
        {
            int objCount = 1;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartArray) objCount++;
                else if (reader.TokenType == JsonToken.EndArray) objCount--;
                if (objCount == 0) break;
            }
        }
    }
        
    public static T DeserializeSource<T>(byte[] data, T instance = null) where T : class, IBinaryDeserializable, new()
    {
        instance = DeserializeFromBytes(data, instance);
        return instance;
    }

    public static T DeserializeFromBytes<T>(byte[] bytes, T instance = null) where T : class, IBinaryDeserializable, new()
    {
        if (instance == null) instance = new T();
        using (var reader = new ZRBinaryReader(new MemoryStream(bytes)))
        {
            instance.Deserialize(reader);
        }

        return instance;
    }

    public static byte[] SerializeToBytes(this IBinarySerializable val)
    {
        using (MemoryStream memStream = new MemoryStream())
        {
            using (var writer = new ZRBinaryWriter(memStream))
            {
                val.Serialize(writer);
            }

            return memStream.ToArray();
        }
    }

    public static string EncodeToString(this IBinarySerializable val)
    {
        byte[] encodedBytes = val.SerializeToBytes();
        string encodedString = Convert.ToBase64String(encodedBytes);
        return encodedString;
    }

    public static T DecodeFromString<T>(string encodedString) where T : class, IBinaryDeserializable, new()
    {
        byte[] bytesEncoded = Convert.FromBase64String(encodedString);
        T val = DeserializeFromBytes<T>(bytesEncoded);
        return val;
    }

    public static byte[] SaveToBinary<T>(this T data) where T : IBinarySerializable
    {
        using var stream = new MemoryStream();
        var writer = new ZRBinaryWriter(stream);
        data.Serialize(writer);
        return stream.ToArray();
    }
    public static void LogCompError<T>(Stack<string> path, string name, Action<string> print, T self, T other)
    {
        print($"{path.Reverse().PrintCollection("/")}/{name} is different, self: {self} other: {other}");
    }
    
    public static void UpdateFrom(this byte[] data, byte[] other, ZRUpdateFromHelper __helper)
    {
        Array.Copy(other, data, other.Length);
    }

    public static void CompareCheck(this byte[] bytes, byte[] bytesOTher, Stack<string> path, Action<string> printer)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            if (b != bytesOTher[i]) LogCompError(path, i.ToString(), printer, b, bytesOTher[i]);
        }
    }
    
    public static void CompareCheck(this Guid guid, Guid guid2, Stack<string> path, Action<string> printer)
    {
        if (guid.CompareTo(guid2) != 0)
        {
            LogCompError(path, "guid", printer, guid, guid2);
        }
    }

    public static bool CompareNullable<T>(Stack<string> path, string name, Action<string> printer, T? val, T? val2)
    {
        if (val == null && val2 == null)
        {
            return false;
        }

        if (val != null && val2 != null)
        {
            return true;
        }

        Func<T, string> pr = t => t == null ? "null" : "not null";
        printer($"{path.Reverse().PrintCollection("/")}/{name} is different, self: {pr(val)} other: {pr(val2)}");
        return false;
    }
    // Return if needs to check further
    public static bool CompareNull<T>(Stack<string> path, string name, Action<string> printer, T val, T val2)
        where T : class
    {
        if (val == null && val2 == null)
        {
            return false;
        }

        if (val != null && val2 != null)
        {
            return true;
        }

        Func<T, string> pr = t => t == null ? "null" : "not null";
        printer($"{path.Reverse().PrintCollection("/")}/{name} is different, self: {pr(val)} other: {pr(val2)}");

        return false;
    }

    public static bool CompareClassId<T>(Stack<string> path, string name, Action<string> printer, T val, T val2) where T : IPolymorphable
    {
        if (val.GetClassId() != val2.GetClassId())
        {
            Func<T, string> pr = t => t.GetClassId().ToString();
            printer($"{path.Reverse().PrintCollection("/")}/{name} class id do not mach, self: {pr(val)} other: {pr(val2)}");
            return false;
        }

        return true;
    }

    public static bool CompareRefs<T>(Stack<string> path, string name, Action<string> printer, T val, T val2)
    {
        if (object.ReferenceEquals(val, val2))
        {
            printer($"{path.Reverse().PrintCollection("/")}/{name} class refs do not match");
            return false;
        }

        return true;
    }

    public static byte[] ReadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;
        using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            byte[] bytes = reader.ReadByteArray();
            return bytes;
        }
    }

    public static T ReadSerializable<T>(this ZRBinaryReader r) where T : IBinaryDeserializable, new()
    {
        var val = new T();
        val.Deserialize(r);
        return val;
    }

    public static void Write(this ZRBinaryWriter r, IBinarySerializable data)
    {
        data.Serialize(r);
    }

    public static string ToBase64(this byte[] bytes)
    {
        return Convert.ToBase64String(bytes);
    }

    public static byte[] FromBase64(this string str)
    {
        return Convert.FromBase64String(str);
    }

    static Dictionary<Type, Func<BinaryReader, object>> readers = new Dictionary<Type, Func<BinaryReader, object>>()
    {
        { typeof(byte), reader => reader.ReadByte() },
        { typeof(ushort), reader => reader.ReadUInt16() },
        { typeof(short), reader => reader.ReadInt16() },
        { typeof(uint), reader => reader.ReadUInt32() },
        { typeof(int), reader => reader.ReadInt32() },
        { typeof(ulong), reader => reader.ReadUInt64() },
        { typeof(long), reader => reader.ReadInt64() },
    };

    public static T ReadEnum<T>(this BinaryReader stream)
    {
        Type t = Enum.GetUnderlyingType(typeof(T));
        object val = readers[t](stream);
        return (T)val;
    }

    public static ulong? ReadNullableUInt64(this BinaryReader reader) =>
        reader.ReadBoolean() ? reader.ReadUInt64() : (ulong?)null;

    public static uint? ReadNullableUInt32(this BinaryReader reader) =>
        reader.ReadBoolean() ? reader.ReadUInt32() : (uint?)null;

    public static void Write(this BinaryWriter writer, ulong? val)
    {
        if (val == null) writer.Write(false);
        else
        {
            writer.Write(true);
            writer.Write((ulong)val);
        }
    }
    
    public static ulong CalculateHash(this Guid val, ZRHashHelper _)
    {
        return (ulong) val.ToString().CalculateHash();
    }

    public static void Write(this BinaryWriter writer, Guid val)
    {
        writer.WriteByteArray(val.ToByteArray());
    }
    
    public static Guid ReadGuid(this BinaryReader reader)
    {
        return new Guid(reader.ReadByteArray());
    }

    public static byte[] ReadByteArray(this BinaryReader stream)
    {
        int size = stream.ReadInt32();
        return stream.ReadBytes(size);
    }

    public static void WriteByteArray(this BinaryWriter stream, byte[] bytes)
    {
        int size = bytes.Length;
        stream.Write(size);
        stream.Write(bytes);
    }

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

    public static void ReadFromStream<T>(this List<T> data, ZRBinaryReader stream) where T : IBinaryDeserializable, new()
    {
        var size = stream.ReadInt32();
        data.Capacity = size;
        for (int q = 0; q < size; q++)
        {
            data.Add(stream.ReadSerializable<T>());
        }
    }

    public static void WriteToStream<T>(this List<T> data, ZRBinaryWriter stream) where T : IBinarySerializable, new()
    {
        ushort size = (ushort)data.Count;
        stream.Write(size);
        data.Capacity = size;
        for (int q = 0; q < size; q++)
        {
            stream.Write(data[q]);
        }
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
            writer.WritePropertyName(ClassIdName);
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

    public static void ReadFromJson<T>(this List<T> self, ZRJsonTextReader reader, Func<ushort, T> constructor)
        where T : class, IJsonSerializable
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Bad Json Format");
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray)
            {
                break;
            }

            var val = constructor(reader.ReadJsonClassId());
            val.ReadFromJson(reader);
            self.Add(val);
        }
    }

    public static void WriteJson<T>(this List<T> self, ZRJsonTextWriter writer) where T : IJsonSerializable
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            self[i].WriteJson(writer);
        }

        writer.WriteEndArray();
    }

    public static void ReadFromJson<T>(this T t, ZRJsonTextReader reader) where T : IJsonSerializable
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var name = (string)reader.Value;
                reader.Read();
                if (!t.ReadFromJsonField(reader, name))
                {
                    reader.SkipObj();
                }
            }
            else if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }
        }
    }

    public static void ReadAssertPropertyName(this JsonTextReader reader, string prop)
    {
        reader.Read();
        if (reader.TokenType != JsonToken.PropertyName || (string)reader.Value != prop)
        {
            throw new ZergRushException("expected a property with name: " + prop);
        }
    }

    public static ushort ReadJsonClassId(this JsonTextReader reader)
    {
        reader.Read();
        if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == ClassIdName)
        {
            return (ushort)reader.ReadAsInt32();
        }
        else
        {
            throw new ZergRushException("error while reading class id in json");
        }

        return 0;
    }

    public static void AddConfigToRegister(this ConfigRegister register, IUniquelyIdentifiable config)
    {
        var id = config.UId();
        if (register.ContainsKey(id))
        {
            throw new ZergRushException($"config {config} has same id {id} as {register[id]} that was already added");
        }

        register.Add(id, config);
    }

    public static string SaveToJsonString<T>(this T data, bool formatting = true) where T : IJsonSerializable
    {
        try
        {
            using (var stream = new StringWriter())
            {
                var writer = new ZRJsonTextWriter(stream);
                writer.Formatting = formatting ? Formatting.Indented : Formatting.None;
                data.WriteJson(writer);
                return stream.ToString();
            }
        }
        catch (Exception e)
        {
            throw;
        }
    }

    public static T LoadFromJsonString<T>(this string content, T data = null) where T : class, IJsonSerializable, new()
    {
        if (data == null) data = new T();
        using (var reader = new StringReader(content))
        {
            data.ReadFromJson(new ZRJsonTextReader(reader));
        }

        return data;
    }

    public static bool LoadFromBinary<T>(this T t, byte[] content)
        where T : IBinaryDeserializable
    {
        try
        {
            using (var reader = new MemoryStream(content))
            {
                t.Deserialize(new ZRBinaryReader(reader));
            }
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }
}