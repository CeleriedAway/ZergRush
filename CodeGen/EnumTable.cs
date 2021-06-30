using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ZergRush;

#if UNITY_EDITOR

namespace ZergRush.CodeGen
{
    public partial class EnumTable
    {
        public Dictionary<string, int> records = new Dictionary<string, int>();

        public static void PrintEnum(IBuilder sink, string enumName, IEnumerable<string> values,
            Func<string, int> valFactory = null, string enumType = "ushort", string comment = null)
        {
            if (comment != null)
                sink.content($"/* {comment} */");
            sink.content($"public enum {enumName} : {enumType}");
            sink.content($"{{");
            sink.indent++;
            bool first = true;
            foreach (var type in values)
            {
                if (valFactory == null)
                {
                    sink.content($"{type}{(first ? "= 1" : "")},");
                }
                else
                {
                    sink.content($"{type} = {valFactory(type)},");
                }

                first = false;
            }

            sink.indent--;
            sink.content($"}}");
        }

        public static void MakeAndSaveEnum(string enumName, List<string> values, string genScriptFolderWithSlashAtTheEnd,
            GeneratorContext context = null, string comment = null)
        {
            var infoCacheFilePath = genScriptFolderWithSlashAtTheEnd + enumName + "ValueCache.txt";
            var typeTable = Load(infoCacheFilePath);
            typeTable.UpdateWithNewTypes(values);
            Save(infoCacheFilePath, typeTable);

            bool contextWasNull = context == null;
            if (contextWasNull)
                context = new GeneratorContext(new GenInfo { sharpGenPath = genScriptFolderWithSlashAtTheEnd }, false);
            var commandTableModule = context.createSharpCustomModule(enumName);
            PrintEnum(commandTableModule, enumName, values, key => typeTable.records[key], comment: comment);
            if (contextWasNull)
                context.Commit();
        }

        public void UpdateWithNewTypes(IEnumerable<string> typesEnumerable)
        {
            var types = new HashSet<string>(typesEnumerable);
            // remove old types
            var oldTypes = records.Keys.ToArray();
            foreach (var oldType in oldTypes)
            {
                if (types.Contains(oldType) == false)
                {
                    records.Remove(oldType);
                }
            }

            var occupiedSlots = records.Values.ToList();
            occupiedSlots.Sort();
            List<int> freeSlots = new List<int>();
            int prevVal = 0;
            for (var i = 0; i < occupiedSlots.Count; i++)
            {
                var slot = occupiedSlots[i];
                for (var j = prevVal + 1; j < slot; j++)
                {
                    freeSlots.Add(j);
                }

                prevVal = slot;
            }

            foreach (var type in types)
            {
                var name = type;
                if (records.ContainsKey(name))
                {
                    // do nothing if type is in table
                }
                else
                {
                    // new type
                    if (freeSlots.Count > 0)
                        records.Add(type, freeSlots.TakeFirst());
                    else
                        records.Add(type, records.Count + 1);
                }
            }
        }

        public static void Save(string fileName, EnumTable table)
        {
            try
            {
                //File.Create(fileName).WriteByte(1);
                var list = table.records.ToList();
                list.Sort((p1, p2) => p1.Value.CompareTo(p2.Value));
                using (TextWriter writer = File.CreateText(fileName))
                {
                    foreach (var record in list)
                    {
                        writer.WriteLine($"{record.Key} {record.Value}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("saving type table exception");
                Debug.LogError(e);
            }
        }

        public static EnumTable Load(string fileName)
        {
            try
            {
                var table = new EnumTable();
                using (TextReader reader = File.OpenText(fileName))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var record = line.Split(' ');
                        table.records.Add(record[0], int.Parse(record[1]));
                    }
                }

                return table;
            }
            catch (Exception e)
            {
                Debug.Log("loading type table exception");
                Debug.Log(e);
            }

            return new EnumTable();
        }
    }
}

#endif