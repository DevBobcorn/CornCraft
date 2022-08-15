using System.IO;
using UnityEngine;

namespace MinecraftClient
{
    public class PathHelper
    {
        public static string GetRootDirectory()
        {
            return Directory.GetParent(Application.dataPath).ToString().Replace('\\', '/');
        }

        public static string GetPacksDirectory()
        {
            return Directory.GetParent(Application.dataPath).ToString().Replace('\\', '/') + "/Resource Packs";
        }

        public static string GetPackDirectoryNamed(string packName)
        {
            return Directory.GetParent(Application.dataPath).ToString().Replace('\\', '/') + "/Resource Packs/" + packName;
        }

        public static string GetExtraDataDirectory()
        {
            return Directory.GetParent(Application.dataPath).ToString().Replace('\\', '/') + "/Extra Data";
        }

        public static string GetExtraDataFile(string fileName)
        {
            return Directory.GetParent(Application.dataPath).ToString().Replace('\\', '/') + "/Extra Data/" + fileName;
        }

    }
}