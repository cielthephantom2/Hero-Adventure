using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Seiryu.Wulin.NET6;

namespace Seiryu.WulinMod
{
    internal static class VideoManager
    {
        public static List<string> Videos = new List<string>();
        public static void Load()
        {

            string path2 = Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_NAME,"Video");
            if(!Directory.Exists(path2))
            {
                Directory.CreateDirectory(path2);
            }
            string[] file2 = Directory.GetFiles(path2, "*.mp4");
            VideoManager.Videos.AddRange(file2);

        }
    }
}