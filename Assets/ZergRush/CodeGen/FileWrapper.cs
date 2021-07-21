using System.IO;
using System.Linq;
using UnityEngine;
using ZergRush.Alive;

// Local wrapper on file manipulations to be crossplatform 
namespace ZergRush
{
    public static class FileWrapper
    {
        public static bool Exists(string filename)
        {
            return File.Exists(PathForPersistentData(filename));
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
                return BuildsPath + fileName;
            else
                return Application.persistentDataPath + "/" + fileName;
        }
        
        public static string BuildsPath
        {
            get
            {
                string absPath = Application.dataPath + "/../";
                if (Application.isEditor)
                {
#if SERVER_ONLY
                    absPath += "../"; // Go up from /Server folder to general project.
#endif
                    string editorPath = "Builds/";
                    absPath += editorPath;
                }
                else
                {
                    if (Application.isConsolePlatform == false && Application.isMobilePlatform == false)
                        absPath += "../";
                    else
                        throw new ZergRushException("Builds path defined only for pc.");
                }
                return absPath;
            }
        }
    }
}