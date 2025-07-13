using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using GameData;
using WuLin;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using TMPro;

namespace Seiryu.WulinMod;

[BepInPlugin("com.seiryu.WulinTH", "WulinTH", "0.0.4")]

public class BepInExLoader : BasePlugin
{
    public static string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    public static Dictionary<string, string> translationDict;
    public static BepInEx.Logging.ManualLogSource log;
    public static BepInExLoader Inst;
    public override void Load()
    {
        BepInExLoader.Inst = this;
        Log.LogInfo($"Plugin {BepInEx.Paths.PluginPath} is loaded!");
        WulinModConfig.LoadConfigFile();
        WulinModConfig.LoadBundleFile();
        WulinModConfig.LoadFontFile();
        WulinModConfig.CreateTMP("FontModded");
        VideoManager.Load();
        if (WulinModConfig.MainFontAsset != null)
        {
            TMP_Settings s_Instance = TMP_Settings.s_Instance;
            TMP_FontAsset defaultFontAsset = s_Instance.m_defaultFontAsset;
            if (defaultFontAsset != null && defaultFontAsset.name != WulinModConfig.MainFontAsset.name)
            {
                s_Instance.m_defaultFontAsset = WulinModConfig.MainFontAsset;
                if (s_Instance.m_fallbackFontAssets == null)
                {
                    s_Instance.m_fallbackFontAssets = new List<TMP_FontAsset>();
                }
                WulinModConfig.CheckAndAddFontFallback(s_Instance.m_fallbackFontAssets, defaultFontAsset);
            }
        }
        BepInExLoader.translationDict = JSONFileNONLocDataToDictionary("LocData.json");
        Harmony harmony = new Harmony("com.seiryu.WulinTH");
        harmony.PatchAll(typeof(Patches));


        
    }

    public static Dictionary<string, string> JSONFileNONLocDataToDictionary(string file)
    {
        string filePath = Path.Combine(BepInExLoader.assemblyFolder, "Translations", file);

        if (!File.Exists(filePath))
        {
            return null;
        }
        
        Dictionary<string, string> dictionary = new Dictionary<string, string>();


        string json = File.ReadAllText(filePath);
        JArray jsonArray = JArray.Parse(json);


        foreach (JObject jsonObject in jsonArray)
        {
            string key = jsonObject["key"].ToString();
            string translation = jsonObject["Translated"].ToString();

            dictionary[key] = translation;
        }

        return dictionary;
    }

}
