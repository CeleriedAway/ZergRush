﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using ZergRush;

public static partial class SerializationFileTools
{
    public static void CheckSerialization<T>(T c) where T : class, IJsonSerializable, ICompareChechable<T>, new()
    {
        var str = new StringWriter();
        var writer = new ZRJsonTextWriter(str);
        c.WriteJson(writer);
        var result = str.ToString();

        var reader = new ZRJsonTextReader(new StringReader(result));
        var c2 = reader.ReadAsJsonRoot<T>();

        c.CompareCheck(c2);
    }
    
    public static T LoadFromBinary<T>(this byte[] content, Func<T> defaultIfLoadFailed = null)
        where T : IBinaryDeserializable, new()
    {
        var data = new T();
        try
        {
            using (var reader = new MemoryStream(content))
            {
                data.Deserialize(new ZRBinaryReader(reader));
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

    public static T ReadFromResourcesSerializable<T>(string filePath, T instance = null)
        where T : class, IBinaryDeserializable, new()
    {
        var asset = Resources.Load<TextAsset>(filePath);
        instance = SerializationTools.DeserializeFromBytes(asset.bytes, instance);
        return instance;
    }

    public static void SaveToFile(this IBinarySerializable obj, string filePath, bool wrapfile)
    {
        using (var writer = new ZRBinaryWriter(OpenFileWrap(filePath, FileMode.Create, wrapfile)))
        {
            obj.Serialize(writer);
        }
    }


    public static void SaveToBinaryFile<T>(this T data, string path) where T : IBinarySerializable
    {
        try
        {
            Debug.Log($"Saving {data} to : " + path);
            using (var file = FileWrapper.Open(path, FileMode.Create))
            {
                data.Serialize(new ZRBinaryWriter(file));
                file.Flush();
                file.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data to path {data} with error : {e.ToError()}");
        }
    }

    public static bool LoadFromBinaryFile<T>(this T data, string path) where T : IBinaryDeserializable
    {
        try
        {
            using (var file = FileWrapper.Open(path, FileMode.Open))
            {
                data.Deserialize(new ZRBinaryReader(file));
                if (data is ILivable livable && livable.isAlive == false) livable.Enlive();
            }
            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log($"Failed to load {typeof(T)} from path: " + path + e.ToError());
            return false;
        }
    }
    public static bool LoadFromBinaryFile<T>(string path, out T data) where T : IBinaryDeserializable, new()
    {
        data = new T();
        try
        {
            using (var file = FileWrapper.Open(path, FileMode.Open))
            {
                data.Deserialize(new ZRBinaryReader(file));
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
            Debug.Log($"Saving {data} to : " + path);
            using (var file = FileWrapper.CreateText(path))
            {
                var writer = new ZRJsonTextWriter(file);
                writer.Formatting = formatting ? Formatting.Indented : Formatting.None;
                data.WriteJson(writer);
                file.Flush();
                file.Close();
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(
                $"Failed to save data {data} to path {path} with error :{e.ToError()}");
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
                data.ReadFromJson(new ZRJsonTextReader(file));
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


    public static bool ReadFromJsonFile<T>(string filePath, T data) where T : class, IJsonSerializable
    {
        try
        {
            using (var file = FileWrapper.OpenText(filePath))
            {
                data.ReadFromJson(new ZRJsonTextReader(file));
                if (data is ILivable livable && livable.isAlive == false) livable.Enlive();
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Failed to load {typeof(T)} from file: " + filePath);
            Debug.Log(e.ToError());
            return false;
        }
        return true;
    }

    public static T ReadFromFile<T>(string filePath, bool wrapPath, T instance = null)
        where T : class, IBinaryDeserializable, new()
    {
        using (var reader = new ZRBinaryReader(OpenFileWrap(filePath, FileMode.Open, wrapPath)))
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