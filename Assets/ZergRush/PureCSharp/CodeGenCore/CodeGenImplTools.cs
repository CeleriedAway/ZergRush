using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ZergRush;
using ZergRush.Alive;

public static class CodeGenImplTools
{
    public static string ClassIdName = "__classId";

    public static void LogCompError<T>(Stack<string> path, string name, Action<string> print, T self, T other)
    {
        print($"{path.Reverse().PrintCollection("/")}/{name} is different, self: {self} other: {other}");
    }

    public static void StableUpdateFrom<T>(this IList<T> self, IList<T> other, ZRUpdateFromHelper __helper) 
        where T : class, IStableIdentifiable, IUpdatableFrom<T>
    {
        var i = 0;
        for (; i < other.Count; i++)
        {
            var currOtherItem = other[i];
            if (currOtherItem == null)
            {
                if (self.Count > i)
                {
                    if (self[i] == null)
                    {
                        continue;
                    }
                    else
                    {
                        self.Insert(i, null);
                        continue;
                    }
                }
            }
            var selfMatchingItemIndex = self.IndexOf(o => o != null && o.stableId == currOtherItem.stableId);
            
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

    public static bool CompareNullable<T>(Stack<string> path, string name, Action<string> printer, T val, T val2)
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
}