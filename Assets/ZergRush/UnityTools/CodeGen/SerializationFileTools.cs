using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using ZergRush;

public static partial class SerializationFileTools
{
    static FileStream OpenFileWrap(string path, FileMode mode, bool wrap)
    {
        return wrap ? UnityFileWrapper.Open(path, mode) : File.Open(path, mode);
    }

    public static void WriteToBinaryFile<T>(this T data, string path, bool unityWrapPath = true)
        where T : IBinarySerializable
    {
        using var file = OpenFileWrap(path, FileMode.Create, unityWrapPath);
        using var zrBinaryWriter = new ZRBinaryWriter(file);
        data.Serialize(zrBinaryWriter);
        file.Flush();
        file.Close();
    }

    public static T ReadFromBinaryFile<T>(this string path, T instance = default, bool unityWrapPath = true)
        where T : IBinaryDeserializable, new()
    {
        using var file = OpenFileWrap(path, FileMode.Open, unityWrapPath);
        using var zrBinaryReader = new ZRBinaryReader(file);
        instance ??= new T();
        instance.Deserialize(zrBinaryReader);
        return instance;
    }

    public static void WriteToJsonFile<T>(this T data, string path, bool formatting = true, bool unityWrapPath = true)
        where T : IJsonSerializable
    {
        using var file = unityWrapPath ? UnityFileWrapper.CreateText(path) : File.CreateText(path);
        using var writer = new ZRJsonTextWriter(file);
        writer.Formatting = formatting ? Formatting.Indented : Formatting.None;
        data.WriteJson(writer);
        file.Flush();
        file.Close();
    }

    public static T ReadFromJsonFile<T>(this string filePath, T instance = default, bool unityWrapPath = true)
        where T : IJsonSerializable, new()
    {
        using var file = unityWrapPath ? UnityFileWrapper.OpenText(filePath) : File.OpenText(filePath);
        instance ??= new T();
        instance.ReadFromJson(new ZRJsonTextReader(file));
        return instance;
    }

    public static bool TryReadFromBinaryFile<T>(this string filePath, T inst, bool printError = false, bool unityWrapPath = true)
        where T : IBinaryDeserializable, new()
    {
        try
        {
            ReadFromBinaryFile(filePath, inst, unityWrapPath);
            return true;
        }
        catch (Exception e)
        {
            if (printError)
            {
                Debug.LogError($"Failed to read {inst.GetType()} binary file at {filePath} with error: {e.ToError()}");
            }
            return false;
        }
    }

    public static bool TryReadFromBinaryFile<T>(this string filePath, out T result, bool unityWrapPath = true)
        where T : IBinaryDeserializable, new()
    {
        result = new T();
        return TryReadFromBinaryFile(filePath, result, unityWrapPath);
    }

    public static bool TryReadFromJsonFile<T>(this string filePath, T inst, bool printError = false, bool unityWrapPath = true)
        where T : IJsonSerializable, new()
    {
        try
        {
            ReadFromJsonFile(filePath, inst, unityWrapPath);
            return true;
        }
        catch (Exception e)
        {
            if (printError)
            {
                Debug.LogError($"Failed to read {inst.GetType()} json file {filePath} with error: {e.ToError()}");
            }
            return false;
        }
    }

    public static bool TryReadFromJsonFile<T>(this string filePath, out T result, bool unityWrapPath = true)
        where T : IJsonSerializable, new()
    {
        result = new T();
        return TryReadFromJsonFile(filePath, result, unityWrapPath);
    }
}