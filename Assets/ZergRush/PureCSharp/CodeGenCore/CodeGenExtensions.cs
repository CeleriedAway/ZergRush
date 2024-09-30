using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ZergRush;
using ZergRush.CodeGen;

public static class CodeGenExtensions
{
    public static ulong CalculateHash(this IHashable t)
    {
        return t.CalculateHash(new ZRHashHelper());
    }

    public static void CompareCheck<T>(this T t, T other) where T : ICompareCheckable<T>
    {
        t.CompareCheck(other, new ZRCompareCheckHelper(), LogSink.errLog);
    }

    public static void UpdateFrom<T>(this T t, T other) where T : IUpdatableFrom<T>
    {
        t.UpdateFrom(other, new ZRUpdateFromHelper());
    }
    
    public static T Read<T>(this byte[] bytes) where T : class, IBinaryDeserializable, new()
    {
        var instance = new T();
        using var reader = new ZRBinaryReader(new MemoryStream(bytes));
        instance.Deserialize(reader);
        return instance;
    }

    public static T Read<T>(this byte[] bytes, T instance) where T : class, IBinaryDeserializable
    {
        using var reader = new ZRBinaryReader(new MemoryStream(bytes));
        instance.Deserialize(reader);
        return instance;
    }
    
    public static T Read<T>(this MemoryStream stream) where T : class, IBinaryDeserializable, new()
    {
        var instance = new T();
        using var reader = new ZRBinaryReader(stream);
        instance.Deserialize(reader);
        return instance;
    }
    
    public static T Read<T>(this T instance, byte[] bytes) where T : class, IBinaryDeserializable
    {
        using var reader = new ZRBinaryReader(new MemoryStream(bytes));
        instance.Deserialize(reader);
        return instance;
    }
    
    public static void Write(this ZRBinaryWriter r, IBinarySerializable data)
    {
        data.Serialize(r);
    }
    
    public static void Write(this MemoryStream stream, IBinarySerializable data)
    {
        using var writer = new ZRBinaryWriter(stream);
        data.Serialize(writer);
    }
    
    public static void Write(this IBinarySerializable data, MemoryStream stream)
    {
        using var writer = new ZRBinaryWriter(stream);
        data.Serialize(writer);
    }
    
    public static byte[] WriteToByteArray<T>(this ref T data) where T : struct, IBinarySerializable
    {
        using var memStream = new MemoryStream();
        using var writer = new ZRBinaryWriter(memStream);
        data.Serialize(writer);
        return memStream.ToArray();
    }

    public static byte[] WriteToByteArray(this IBinarySerializable data)
    {
        using var memStream = new MemoryStream();
        using var writer = new ZRBinaryWriter(memStream);
        data.Serialize(writer);
        return memStream.ToArray();
    }

    public static string WriteToBase64(this IBinarySerializable val)
    {
        byte[] encodedBytes = val.WriteToByteArray();
        string encodedString = Convert.ToBase64String(encodedBytes);
        return encodedString;
    }

    public static T ReadFromBase64<T>(string encodedString) where T : class, IBinaryDeserializable, new()
    {
        byte[] bytesEncoded = Convert.FromBase64String(encodedString);
        T val = Read<T>(bytesEncoded);
        return val;
    }

    public static T Read<T>(this ZRBinaryReader r) where T : IBinaryDeserializable, new()
    {
        var val = new T();
        val.Deserialize(r);
        return val;
    }

    public static string ToBase64(this byte[] bytes)
    {
        return Convert.ToBase64String(bytes);
    }

    public static byte[] FromBase64(this string str)
    {
        return Convert.FromBase64String(str);
    }

    public static string WriteToJsonString<T>(this T data, bool formatting = true) where T : IJsonSerializable
    {
        using var stream = new StringWriter();
        var writer = new ZRJsonTextWriter(stream);
        writer.Formatting = formatting ? Formatting.Indented : Formatting.None;
        data.WriteJson(writer);
        return stream.ToString();
    }
    
    public static T ReadFromJson<T>(this string content) where T : class, IJsonSerializable, new()
    {
        var data = new T();
        using var reader = new StringReader(content);
        using var zrJsonTextReader = new ZRJsonTextReader(reader);
        data.ReadFromJson(zrJsonTextReader);
        return data;
    }

    public static T ReadFromJson<T>(this T data, string content) where T : class, IJsonSerializable, new()
    {
        if (data == null) data = new T();
        using var reader = new StringReader(content);
        using var zrJsonTextReader = new ZRJsonTextReader(reader);
        data.ReadFromJson(zrJsonTextReader);
        return data;
    }
    
    public static void ReadList<T>(this List<T> data, ZRBinaryReader stream) where T : IBinaryDeserializable, new()
    {
        var size = stream.ReadInt32();
        data.Capacity = size;
        for (int q = 0; q < size; q++)
        {
            data.Add(stream.Read<T>());
        }
    }

    public static void WriteList<T>(this List<T> data, ZRBinaryWriter stream) where T : IBinarySerializable, new()
    {
        ushort size = (ushort)data.Count;
        stream.Write(size);
        data.Capacity = size;
        for (int q = 0; q < size; q++)
        {
            stream.Write(data[q]);
        }
    }
    
    public static void CheckJsonSerialization<T>(this T c) where T : class, IJsonSerializable, ICompareCheckable<T>, new()
    {
        var str = c.WriteToJsonString();
        var c2 = c.ReadFromJson(str);
        c.CompareCheck(c2);
    }
    
    public static void CheckBinarySerialization<T>(this T c) where T : class, IBinarySerializable, 
        IBinaryDeserializable, ICompareCheckable<T>, new()
    {
        var str = new MemoryStream();
        c.Write(str);
        T c2 = new T();
        c2.Read(str.GetBuffer());
        c.CompareCheck(c2);
    }
    
}