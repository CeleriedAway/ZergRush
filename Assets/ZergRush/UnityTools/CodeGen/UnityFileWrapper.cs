﻿using System.IO;
using System.Linq;
using UnityEngine;

// Local wrapper on file manipulations to be crossplatform 
namespace ZergRush
{
    public static class UnityFileWrapper
    {
        public static bool Exists(string filename)
        {
            return File.Exists(PathForPersistentData(filename));
        }

        public static void WriteAllBytes(string filename, byte[] content)
        {
            File.WriteAllBytes(PathForPersistentData(filename), content);
        }
        public static void WriteAllText(string filename, string content)
        {
            File.WriteAllText(PathForPersistentData(filename), content);
        }
        public static void RemoveIfExists(string filename)
        {
            if (Exists(filename))
                File.Delete(PathForPersistentData(filename));
        }
        public static FileStream Open(string filename, FileMode mode)
        {
            return File.Open(PathForPersistentData(filename), mode);
        }
        public static StreamReader OpenText(string filename)
        {
            return File.OpenText(PathForPersistentData(filename));
        }
        public static TextWriter CreateText(string path)
        {
            return File.CreateText(PathForPersistentData(path));
        }

        public static string[] FindLocalFilesWithSuffix(string suffix)
        {
            return Directory.GetFiles(PathForPersistentData(suffix)).Where(n => n.EndsWith(suffix)).ToArray();
        }
    
        public static string PathForPersistentData(string fileName)
        {
            if (Application.isEditor)
                return fileName;
            else if (Application.isConsolePlatform == false && Application.isMobilePlatform == false)
                return Path.Combine(BuildsPath, fileName);
            else
                return Path.Combine(Application.persistentDataPath, fileName);
        }
        
        public static string BuildsPath
        {
            get
            {
                var absPath = Application.dataPath + "/../";
                return absPath;
            }
        }
    }
}