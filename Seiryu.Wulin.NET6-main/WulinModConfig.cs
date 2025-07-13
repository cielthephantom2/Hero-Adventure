using System.IO;
using Il2CppSystem.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;
using TMPro;
using Il2CppInterop.Runtime;

namespace Seiryu.WulinMod;

public class WulinModConfig
{
    public static bool Dump { get; set; }
    public static TMP_FontAsset MainFontAsset { get; set; }
    public static string FontName { get; set; }
    public static string CharAddToAsset { get; set; }
    public static Font MainFont { get; set; }
    public static bool isBundle = false;
    public static Dictionary<string, string> CustomConfig { get; set; }
    public static string pathBundleFont = Path.Combine(BepInExLoader.assemblyFolder, "Font", "thaifont.bundle");
    public static string pathMainFont = Path.Combine(BepInExLoader.assemblyFolder, "Font", "font.ttf");

    private static Dictionary<string, string> GetKeys(string iniFile, string category)
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        string[] array = File.ReadAllLines(iniFile);
        string text = "";
        for (int i = 0; i < array.Length; i++)
        {
            string text2 = array[i].Trim();
            if (!text2.StartsWith("#") && !Utility.IsNullOrWhiteSpace(text2))
            {
                if (text2.StartsWith("[") && text2.EndsWith("]"))
                {
                    text = text2.Substring(1, text2.Length - 2);
                }
                else if (!(text.ToLower() != category.ToLower()))
                {
                    string[] array2 = text2.Split(new char[]
                    {
                            '='
                    }, 2);
                    if (array2.Length == 2)
                    {
                        string text3 = array2[0].Trim();
                        string text4 = array2[1].Trim();
                        dictionary.Add(text3, text4);
                    }
                }
            }
        }
        return dictionary;
    }

    public static void LoadConfigFile()
    {
        string text = Path.Combine(Paths.ConfigPath, "WulinModConfig.cfg");
        ConfigFile configFile = new ConfigFile(text, false);
        configFile.SaveOnConfigSet = false;
        ConfigEntry<bool> configEntryDump = configFile.Bind<bool>("General", "Dump", false, "");
        ConfigEntry<string> configEntryFont = configFile.Bind<string>("General", "FontName", "font in bundle");
        ConfigEntry<string> configEntryChar = configFile.Bind<string>("General", "CharAddToAsset", "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำิีึืฺุู฿เแโใไๅๆ็่้๊๋์ํ๎๏๐๑๒๓๔๕๖๗๘๙๚๛");
        WulinModConfig.Dump = configEntryDump.Value;
        WulinModConfig.FontName = configEntryFont.Value;
        WulinModConfig.CharAddToAsset = configEntryChar.Value;
        WulinModConfig.CustomConfig = WulinModConfig.GetKeys(text, "custom");
    }

    public static void CreateTMP(string sName = "FontModded")
    {
        if (File.Exists(WulinModConfig.pathMainFont))
        {
            TMP_FontAsset tmp_FontAsset = TMP_FontAsset.CreateFontAsset(WulinModConfig.MainFont, 60, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            tmp_FontAsset.hideFlags = HideFlags.HideAndDontSave;
            tmp_FontAsset.name = sName;
            WulinModConfig.MainFontAsset = tmp_FontAsset;
            WulinModConfig.MainFontAsset.fallbackFontAssets = new List<TMP_FontAsset>();
            if (!string.IsNullOrEmpty(WulinModConfig.CharAddToAsset))
            {
                foreach (char character in WulinModConfig.CharAddToAsset.Trim().ToCharArray())
                {
                    WulinModConfig.MainFontAsset.HasCharacter(character, true, false);
                }
            }
        }
        TMP_FontAsset.RegisterFontAssetForAtlasTextureUpdate(WulinModConfig.MainFontAsset);
        TMP_FontAsset.RegisterFontAssetForFontFeatureUpdate(WulinModConfig.MainFontAsset);
        TMP_FontAsset.UpdateAtlasTexturesForFontAssetsInQueue();
        TMP_FontAsset.UpdateFontFeaturesForFontAssetsInQueue();
    }

    public static void LoadBundleFile()
    {
        WulinModConfig.MainFont = new Font(IL2CPP.GetIl2CppClass("UnityEngine.TextRenderingModule.dll", "UnityEngine", "Font"));
        if (File.Exists(WulinModConfig.pathBundleFont))
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(WulinModConfig.pathBundleFont);
            if (WulinModConfig.FontName != "")
            {
                WulinModConfig.MainFont = assetBundle.LoadAsset("assets/" + WulinModConfig.FontName.Trim() + ".ttf").TryCast<Font>();
                WulinModConfig.isBundle = true;
            }
            if (WulinModConfig.MainFont == null)
            {
                WulinModConfig.MainFont = assetBundle.LoadAsset("assets/arial.ttf").TryCast<Font>();
                WulinModConfig.isBundle = true;
            }
        }
    }

    public static void LoadFontFile()
    {
        if (!WulinModConfig.isBundle && File.Exists(WulinModConfig.pathMainFont))
        {
            Font.Internal_CreateFontFromPath(WulinModConfig.MainFont, WulinModConfig.pathMainFont);
            WulinModConfig.MainFont.hideFlags = HideFlags.HideAndDontSave;
            WulinModConfig.MainFont.name = "FontModded";
        }
    }

    public static void CheckAndAddFontFallback(List<TMP_FontAsset> listFont, TMP_FontAsset font)
    {
        if (font == null)
        {
            return;
        }
        bool flag = false;
        List<TMP_FontAsset>.Enumerator enumerator = listFont.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.name == font.name)
            {
                flag = true;
            }
        }
        if (!flag)
        {
            listFont.Add(font);
        }
    }

    public static string GetGameObjectPath(GameObject obj)
    {
        string text = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            text = "/" + obj.name + text;
        }
        return text.Remove(0, 1);
    }
}