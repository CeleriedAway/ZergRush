using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZergRush.Alive;
using Newtonsoft.Json;
using ZergRush;
using UnityEngine;
using ZergRush.ReactiveCore;

public interface ISerializable
{
    void Serialize(BinaryWriter writer);
    void Deserialize(BinaryReader reader);
}

public interface IUniquelyIdentifiable
{
    ulong UId();
}

public interface ILivableModification : IUniquelyIdentifiable
{
    int modificationOwnerRefId { get; set; }
}


public interface ILivable
{
    void Enlive();
    void Mortify();
}

public interface IReferencableFromDataRoot
{
    int Id { get; }
}

public interface IPolymorphable
{
    ushort GetClassId();
}

public interface IHasUpdateEvent
{
    IEventStream Updated { get; }
}

public interface IJsonSerializable
{
    void WriteJsonFields(JsonTextWriter writer);
    void ReadFromJsonField(JsonTextReader reader, string name);
}

public interface IHashable
{
    ulong CalculateHash();
}

public interface IUpdatableFrom<in T>
{
    void UpdateFrom(T val);
}

public interface IPooledUpdatableFrom<T>
{
    void UpdateFrom(T val, ObjectPool pool);
}

public interface ICompareChechable<T>
{
    void CompareCheck(T t, Stack<string> path);
}

public class JsonSerializationException : Exception
{
    public JsonSerializationException(string message) : base(message)
    {
    }
}

public class ZergRushCorruptedOrInvalidDataLayout : ZergRushException
{
    public ZergRushCorruptedOrInvalidDataLayout(string message) : base(message)
    {
    }

    public ZergRushCorruptedOrInvalidDataLayout()
    {
    }
}

public static class SerializationTools
{
    public static T ReadSerializable<T>(this BinaryReader r) where T : ISerializable, new()
    {
        var val = new T();
        val.Deserialize(r);
        return val;
    }
    
    internal static bool Valid(this string str)
    {
        return !String.IsNullOrEmpty(str);
    }

    public static void Write(this BinaryWriter r, ISerializable data)
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
        {typeof(byte), reader => reader.ReadByte()},
        {typeof(ushort), reader => reader.ReadUInt16()},
        {typeof(short), reader => reader.ReadInt16()},
        {typeof(uint), reader => reader.ReadUInt32()},
        {typeof(int), reader => reader.ReadInt32()},
        {typeof(ulong), reader => reader.ReadUInt64()},
        {typeof(long), reader => reader.ReadInt64()},
    };

    public static T ReadEnum<T>(this BinaryReader stream)
    {
        Type t = Enum.GetUnderlyingType(typeof(T));
        object val = readers[t](stream);
        return (T) val;
    }

    public static ulong? ReadNullableUInt64(this BinaryReader reader) =>
        reader.ReadBoolean() ? reader.ReadUInt64() : (ulong?) null;

    public static uint? ReadNullableUInt32(this BinaryReader reader) =>
        reader.ReadBoolean() ? reader.ReadUInt32() : (uint?) null;

    public static void Write(this BinaryWriter writer, ulong? val)
    {
        if (val == null) writer.Write(false);
        else
        {
            writer.Write(true);
            writer.Write((ulong) val);
        }
    }

    public static void Write(this BinaryWriter writer, uint? val)
    {
        if (val == null) writer.Write(false);
        else
        {
            writer.Write(true);
            writer.Write((uint) val);
        }
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

    public static uint CalculateHash(this byte[] array)
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

    public static ulong CalculateHash(this string array)
    {
        ulong hash = 0;
        for (int i = 0; i < array.Length; i++)
        {
            hash += array[i];
            hash += hash << 10;
            hash ^= hash >> 7;
        }

        return hash;
    }

    public static void ReadFromStream<T>(this List<T> data, BinaryReader stream) where T : ISerializable, new()
    {
        var size = stream.ReadInt32();
        data.Capacity = size;
        for (int q = 0; q < size; q++)
        {
            data.Add(stream.ReadSerializable<T>());
        }
    }

    public static void WriteToStream<T>(this List<T> data, BinaryWriter stream) where T : ISerializable, new()
    {
        Debug.Assert(data.Count > UInt16.MaxValue, "writing stream failed");
        ushort size = (ushort) data.Count;
        stream.Write(size);
        data.Capacity = size;
        for (int q = 0; q < size; q++)
        {
            stream.Write(data[q]);
        }
    }

    public static void LogCompError<T>(Stack<string> path, string name, T self, T other)
    {
        Debug.LogError($"{path.Reverse().PrintCollection("/")}/{name} is different, self: {self} other: {other}");
    }

    public static void CompareCheck(this byte[] bytes, byte[] bytesOTher, Stack<string> path)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            if (b != bytesOTher[i]) LogCompError(path, i.ToString(), b, bytesOTher[i]);
        }
    }

    // Return if needs to check further
    public static bool CompareNull<T>(Stack<string> path, string name, T val, T val2) where T : class
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
        Debug.LogError($"{path.Reverse().PrintCollection("/")}/{name} is different, self: {pr(val)} other: {pr(val2)}");

        return false;
    }

    public static bool CompareClassId<T>(Stack<string> path, string name, T val, T val2) where T : IPolymorphable
    {
        if (val.GetClassId() != val2.GetClassId())
        {
            Func<T, string> pr = t => t.GetClassId().ToString();
            Debug.LogError(
                $"{path.Reverse().PrintCollection("/")}/{name} class id do not mach, self: {pr(val)} other: {pr(val2)}");
            return false;
        }

        return true;
    }

    public static void WriteJson(this IJsonSerializable obj, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        var polymorph = obj as IPolymorphable;
        if (polymorph != null)
        {
            writer.WritePropertyName("classId");
            writer.WriteValue(polymorph.GetClassId());
//            var f = writer.Formatting;
//            writer.Formatting = Formatting.None;
//            writer.WritePropertyName("className");
//            writer.WriteValue(obj.GetType().Name);
//            writer.Formatting = f;
        }

        obj.WriteJsonFields(writer);
        writer.WriteEndObject();
    }

    public static T ReadAsJsonRoot<T>(this JsonTextReader reader, T obj = null)
        where T : class, IJsonSerializable, new()
    {
        if (obj == null) obj = new T();
        reader.Read();
        obj.ReadFromJson(reader);
        return obj;
    }

    public static void ReadFromJson<T>(this List<T> self, JsonTextReader reader, Func<ushort, T> constructor)
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

    public static void WriteJson<T>(this List<T> self, JsonTextWriter writer) where T : IJsonSerializable
    {
        writer.WriteStartArray();
        for (int i = 0; i < self.Count; i++)
        {
            self[i].WriteJson(writer);
        }

        writer.WriteEndArray();
    }

    public static void ReadFromJson<T>(this T t, JsonTextReader reader) where T : IJsonSerializable
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var name = (string) reader.Value;
                reader.Read();
                t.ReadFromJsonField(reader, name);
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
        if (reader.TokenType != JsonToken.PropertyName || (string) reader.Value != prop)
        {
            throw new ZergRushException("expected a property with name: " + prop);
        }
    }

    public static ushort ReadJsonClassId(this JsonTextReader reader)
    {
        reader.Read();
        if (reader.TokenType == JsonToken.PropertyName && (string) reader.Value == "classId")
        {
            return (ushort) reader.ReadAsInt32();
        }
        else
        {
            Debug.LogError("error while reading class id in json");
        }

        return 0;
    }

    public static bool CompareRefs<T>(Stack<string> path, string name, T val, T val2)
    {
        if (object.ReferenceEquals(val, val2))
        {
            Debug.LogError($"{path.Reverse().PrintCollection("/")}/{name} class refs do not match");
            return false;
        }

        return true;
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
                var writer = new JsonTextWriter(stream);
                writer.Formatting = formatting ? Formatting.Indented : Formatting.None;
                data.WriteJson(writer);
                return stream.ToString();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data to path {data} with error :");
            Debug.LogError(e.Message + e.StackTrace);
            throw;
        }
    }

    public static T LoadFromJsonString<T>(this string content, T data = null) where T : class, IJsonSerializable, new()
    {
        if (data == null) data = new T();
        try
        {
            using (var reader = new StringReader(content))
            {
                data.ReadFromJson(new JsonTextReader(reader));
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"Failed to load json from string with error :{e.Message} call stack=\n{e.StackTrace}");
        }

        return data;
    }

    public static T LoadFromJsonFile<T>(string path, Func<T> defaultData = null, bool printError = true)
        where T : class, IJsonSerializable, new()
    {
        T data;
        if (!LoadFromJsonFile(path, out data, printError))
            return defaultData != null ? defaultData.Invoke() : new T();
        else
            return data;
    }

    public static bool LoadFromJsonFileIfExists<T>(string path, out T data, bool printError = true)
        where T : class, IJsonSerializable, new()
    {
        if (!FileWrapper.Exists(path))
        {
            data = default;
            return false;
        }
        else
            return LoadFromJsonFile(path, out data, printError);
    }

    public static void SaveJsonToFileRemoveOnNull<T>(this T data, string path, bool formatting = true)
        where T : class, IJsonSerializable
    {
        if (data == null)
            FileWrapper.RemoveIfExists(path);
        else
            data.SaveToJsonFile(path, formatting);
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

    public static T DeserializeSource<T>(byte[] data, T instance = null) where T : class, ISerializable, new()
    {
        instance = DeserializeFromBytes(data, instance);
        return instance;
    }

    public static void SaveToFilePure(this byte[] obj, string filePath, bool wrapPath)
    {
        using (BinaryWriter writer = new BinaryWriter(OpenFileWrap(filePath, FileMode.Create, wrapPath)))
        {
            writer.Write(obj);
        }
    }

    static FileStream OpenFileWrap(string path, FileMode mode, bool wrap)
    {
        return wrap ? FileWrapper.Open(path, mode) : File.Open(path, mode);
    }

    public static T DeserializeFromBytes<T>(byte[] bytes, T instance = null) where T : class, ISerializable, new()
    {
        if (instance == null) instance = new T();
        using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
        {
            instance.Deserialize(reader);
        }

        return instance;
    }

    public static byte[] SerializeToBytes(this ISerializable val)
    {
        using (MemoryStream memStream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(memStream))
            {
                val.Serialize(writer);
            }

            return memStream.ToArray();
        }
    }

    public static T ReadFromResourcesSerializable<T>(string filePath, T instance = null)
        where T : class, ISerializable, new()
    {
        var asset = Resources.Load<TextAsset>(filePath);
        instance = DeserializeFromBytes(asset.bytes, instance);
        return instance;
    }

    public static string EncodeToString(this ISerializable val)
    {
        byte[] encodedBytes = val.SerializeToBytes();
        string encodedString = Convert.ToBase64String(encodedBytes);
        return encodedString;
    }

    public static T DecodeFromString<T>(string encodedString) where T : class, ISerializable, new()
    {
        byte[] bytesEncoded = Convert.FromBase64String(encodedString);
        T val = DeserializeFromBytes<T>(bytesEncoded);
        return val;
    }

    public static void SaveToFile(this ISerializable obj, string filePath, bool wrapfile)
    {
        using (BinaryWriter writer = new BinaryWriter(OpenFileWrap(filePath, FileMode.Create, wrapfile)))
        {
            obj.Serialize(writer);
        }
    }

    public static byte[] SaveToBinary<T>(this T data) where T : ISerializable
    {
        try
        {
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                data.Serialize(writer);
                return stream.ToArray();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data to bytes {data} with error :");
            Debug.LogError(e.Message + e.StackTrace);
            throw;
        }
    }

    public static void CheckSerialization<T>(T c) where T : class, IJsonSerializable, ICompareChechable<T>, new()
    {
        var str = new StringWriter();
        var writer = new JsonTextWriter(str);
        c.WriteJson(writer);
        var result = str.ToString();

        var reader = new JsonTextReader(new StringReader(result));
        var c2 = reader.ReadAsJsonRoot<T>();

        c.CompareCheck(c2, new Stack<string>());
    }

    public static T LoadFromBinary<T>(this byte[] content, Func<T> defaultIfLoadFailed = null)
        where T : ISerializable, new()
    {
        var data = new T();
        try
        {
            using (var reader = new MemoryStream(content))
            {
                data.Deserialize(new BinaryReader(reader));
            }
        }
        catch (Exception e)
        {
            if (defaultIfLoadFailed != null)
                data = defaultIfLoadFailed();
            else
                data = new T();
            Debug.LogError($"Failed to load binary with error :{e.Message} call stack=\n{e.StackTrace}");
        }

        return data;
    }


    public static void SaveToBinaryFile<T>(T data, string path) where T : ISerializable
    {
        try
        {
            Debug.Log($"Saving {data} to : " + path);
            using (var file = FileWrapper.Open(path, FileMode.Create))
            {
                data.Serialize(new BinaryWriter(file));
                file.Flush();
                file.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data to path {data} with error :");
            Debug.LogError(e.Message);
        }
    }

    public static bool LoadFromBinaryFile<T>(string path, out T data) where T : ISerializable, new()
    {
        data = new T();
        try
        {
            using (var file = FileWrapper.Open(path, FileMode.Open))
            {
                data.Deserialize(new BinaryReader(file));
                if (data is ILivable livable) livable.Enlive();
            }

            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log($"Failed to load {typeof(T)} from path: " + path + e.ToError());
            return false;
        }
    }

    public static void SaveToJsonFile<T>(this T data, string path, bool formatting = true)
        where T : IJsonSerializable
    {
        try
        {
            UnityEngine.Debug.Log($"Saving {data} to : " + path);
            using (var file = FileWrapper.CreateText(path))
            {
                var writer = new JsonTextWriter(file);
                writer.Formatting = formatting ? Formatting.Indented : Formatting.None;
                data.WriteJson(writer);
                file.Flush();
                file.Close();
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(
                $"Failed to save data {data} to path {path} with error :{e.Message} stacktrace =\n{e.StackTrace}");
        }
    }

    public static bool LoadFromJsonFile<T>(string path, out T data, bool printError = true)
        where T : class, IJsonSerializable, new()
    {
        try
        {
            using (var file = FileWrapper.OpenText(path))
            {
                data = new T();
                data.ReadFromJson(new JsonTextReader(file));
                if (data is ILivable livable) livable.Enlive();
                return true;
            }
        }
        catch (Exception e)
        {
            if (printError)
            {
                Debug.Log($"Failed to load {typeof(T)} from file: " + path);
                Debug.Log(e.ToString());
            }

            data = null;
            return false;
        }
    }


    public static void ReadFromJsonFile<T>(string filePath, T data) where T : class, IJsonSerializable, new()
    {
        try
        {
            using (var file = FileWrapper.OpenText(filePath))
            {
                data.ReadFromJson(new JsonTextReader(file));
                if (data is ILivable livable) livable.Enlive();
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Failed to load {typeof(T)} from file: " + filePath);
            Debug.Log(e.ToString());
        }
    }

    public static T ReadFromFile<T>(string filePath, bool wrapPath, T instance = null)
        where T : class, ISerializable, new()
    {
        using (BinaryReader reader = new BinaryReader(OpenFileWrap(filePath, FileMode.Open, wrapPath)))
        {
            if (instance == null)
                instance = new T();
            instance.Deserialize(reader);
            return instance;
        }
    }

    public static void SaveToFile(this byte[] obj, string filePath, bool wrapPath)
    {
        using (BinaryWriter writer = new BinaryWriter(OpenFileWrap(filePath, FileMode.Create, wrapPath)))
        {
            writer.WriteByteArray(obj);
        }
    }


//    public static void ReadFromStream<T>(this ReactiveCollection<T> collection, BinaryReader stream) where T : ISerializable, new()
//    {
//        var data = new List<T>();
//        data.ReadFromStream(stream);
//        collection.Reset(data);
//    }
//    
//    public static void WriteToStream<T>(this ReactiveCollection<T> data, BinaryWriter stream) where T : ISerializable, new()
//    {
//        stream.Write(data.current);
//    }
}