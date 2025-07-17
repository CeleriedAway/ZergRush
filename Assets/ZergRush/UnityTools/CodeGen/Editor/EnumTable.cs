using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZergRush;

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

        public static void MakeAndSaveSimpleEnum(
            string enumName,
            List<(string, int)> values,
            string genScriptFolderWithSlashAtTheEnd,
            GeneratorContext context = null, string comment = null)
        {
            var t = new EnumTable();
            foreach (var valueTuple in values)
            {
                t.records.Add(valueTuple.Item1, valueTuple.Item2);
            }
            GenEnum(t, enumName, genScriptFolderWithSlashAtTheEnd, context, comment);
        }

        static void GenEnum(EnumTable table, string enumName, string genScriptFolderWithSlashAtTheEnd, GeneratorContext context = null, string comment = null)
        {
            bool contextWasNull = context == null;
            if (contextWasNull)
                context = new GeneratorContext(new GenInfo { sharpGenPath = genScriptFolderWithSlashAtTheEnd }, false);
            var commandTableModule = context.createSharpCustomModule(enumName, "enum");
            PrintEnum(commandTableModule, enumName, table.records.Keys, key => table.records[key], comment: comment);
            if (contextWasNull)
                context.Commit();
        }

        public static void MakeAndSaveEnumWithCachedValues(string enumName, List<string> values, string genScriptFolderWithSlashAtTheEnd,
            GeneratorContext context = null, string comment = null)
        {
            int tries = 0;
            string currentPath = ".";
            while (Directory.Exists(Path.Combine(currentPath, "ProjectSettings")) == false)
            {
                // Console.WriteLine($"{currentPath} has {Directory.GetFiles(currentPath).PrintCollection()}");

                currentPath += $"{Path.DirectorySeparatorChar}..";
                tries++;

                if (tries > 30)
                {
                    throw new ArgumentException($"Cant find project root folder, current path {currentPath}");
                }
            }
            
            var infoCacheFilePath = Path.Combine(currentPath, genScriptFolderWithSlashAtTheEnd) + enumName + "ValueCache.txt";
            var typeTable = Load(infoCacheFilePath);
            typeTable.UpdateWithNewTypes(values);
            SaveEnumCache(infoCacheFilePath, typeTable);
            GenEnum(typeTable, enumName, genScriptFolderWithSlashAtTheEnd, context, comment);
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

        public static void SaveEnumCache(string fileName, EnumTable table)
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
                LogSink.errLog?.Invoke("saving type table exception " + e.Message);
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
                // ignore
            }

            return new EnumTable();
        }
    }
}
