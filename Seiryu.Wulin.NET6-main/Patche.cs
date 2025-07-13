using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using GameData;
using WuLin;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Video;

namespace Seiryu.WulinMod;

class Patches
{

    private static System.Random random = new System.Random();
    
    private static void SetVideo(VideoPlayer vd)
    {
        if (VideoManager.Videos.Count == 0)
        {
            return;
        }
        vd.playOnAwake = true;
        vd.isLooping = false;
        vd.source = (VideoSource)1;
        vd.controlledAudioTrackCount = 1;
        AudioSource audioSource = vd.gameObject.AddComponent<AudioSource>();
        vd.SetTargetAudioSource(0, audioSource);
        vd.url = "file://" + VideoManager.Videos[0];
        vd.Prepare();
        vd.Play();
    }


    [HarmonyPatch(typeof(UIVideoPlayer), "ShowAsync")]
    [HarmonyPostfix]
    private static void UIVideoPlayer_Play(UIVideoPlayer __instance, ref string path)
    {
        if (__instance.path == "Video/AfterCreatePlayer_en.mp4")
            SetVideo(__instance.ScreenVideoPlayer);

    }

    [HarmonyPatch(typeof(GameLoc), "GetLocString")]
    [HarmonyPostfix]
    private static void GameLoc__GetLocString_Postfix(GameLoc __instance, string key, ref bool enableSplit, ref string __result)
    {

        if (BepInExLoader.translationDict == null)
            return;

        if (BepInExLoader.translationDict.ContainsKey(key))
        {
            if (BepInExLoader.translationDict[key] == "")
                return;

            if (BepInExLoader.translationDict[key].Contains("|"))
            {
                string[] sentences = BepInExLoader.translationDict[key].Split('|');
                int randomIndex = random.Next(0, sentences.Length);
                __result = sentences[randomIndex].Trim();
            }
            else
            {
                __result = BepInExLoader.translationDict[key];
            }
        }

    }

    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    [HarmonyPriority(800)]
    [HarmonyPostfix]
    private static void TextMeshProUGUI_OnEnable_Postfix(TextMeshProUGUI __instance)
    {

        if (WulinModConfig.MainFontAsset != null && WulinModConfig.MainFontAsset.name != __instance.font.name)
        {
            TMP_FontAsset font = __instance.font;

            if (__instance.font.fallbackFontAssets == null)
            {
                __instance.font.fallbackFontAssets = new Il2CppSystem.Collections.Generic.List<TMP_FontAsset>();
            }
            WulinModConfig.CheckAndAddFontFallback(__instance.font.fallbackFontAssets, font);
            __instance.font = WulinModConfig.MainFontAsset;

        }

        string gameObjectPath = WulinModConfig.GetGameObjectPath(__instance.gameObject);
        foreach (Il2CppSystem.Collections.Generic.KeyValuePair<string, string> customConfigDict in WulinModConfig.CustomConfig)
        {
            bool flag = customConfigDict.Key.Contains("$");
            GameObject gameObject = flag
                ? GameObject.Find(customConfigDict.Key.Replace("$", ""))
                : __instance.gameObject;
            System.Collections.Generic.IEnumerable<string> sourceElements = customConfigDict.Key
                .Replace("$", "")
                .Split('|', System.StringSplitOptions.None);
            bool isMatchingElements = sourceElements.All(x => gameObjectPath.ToLower().Contains(x.Trim().ToLower()));
            if (isMatchingElements)
            {
                string[] settings = customConfigDict.Value.Split(',', System.StringSplitOptions.None)
                    .Select(x => x.Trim().ToLower())
                    .ToArray();
                foreach (string setting in settings)
                {
                    switch (setting)
                    {
                        case string s when s.Contains("sizedelta"):
                            float x = gameObject.GetComponent<RectTransform>().sizeDelta.x;
                            float y = gameObject.GetComponent<RectTransform>().sizeDelta.y;
                            foreach (string element in setting.Replace("sizedelta", "").Trim().Split(' ', System.StringSplitOptions.None))
                            {
                                float.TryParse(element.Replace("x", "").Replace("y", ""), out float number);
                                if (element.Contains("x"))
                                    x = Mathf.Approximately(0f, number) ? x : number;
                                else if (element.Contains("y"))
                                    y = Mathf.Approximately(0f, number) ? y : number;
                            }
                            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(x, y);
                            break;
                        case string s when s.Contains("localposition"):
                            float xPos = gameObject.transform.localPosition.x;
                            float yPos = gameObject.transform.localPosition.y;
                            float zPos = gameObject.transform.localPosition.z;
                            foreach (string element in setting.Replace("localposition", "").Trim().Split(' ', System.StringSplitOptions.None))
                            {
                                float.TryParse(element.Replace("x", "").Replace("y", "").Replace("z", ""), out float number);
                                if (element.Contains("x"))
                                    xPos = Mathf.Approximately(0f, number) ? xPos : number;
                                else if (element.Contains("y"))
                                    yPos = Mathf.Approximately(0f, number) ? yPos : number;
                                else if (element.Contains("z"))
                                    zPos = Mathf.Approximately(0f, number) ? zPos : number;
                            }
                            gameObject.transform.localPosition = new Vector3(xPos, yPos, zPos);
                            break;
                        case string s when s.Contains("fontsize"):
                            string sizeValue = setting.Replace("fontsize", "").Trim();
                            int size = sizeValue.Contains('%')
                                ? Mathf.RoundToInt(int.Parse(sizeValue.Replace("%", "")) * __instance.fontSize / 100f)
                                : (int.TryParse(sizeValue, out int fontSizeVal) ? fontSizeVal : 0);
                            if (size > 0)
                                __instance.fontSize = size;
                            break;
                        case "enableautosizing":
                            __instance.enableAutoSizing = true;
                            break;

                        case "nonenableautosizing":
                            __instance.enableAutoSizing = false;
                            break;
                        case "autosizetextcontainer":
                            __instance.autoSizeTextContainer = true;
                            break;
                        case string s when s.Contains("fsmin"):
                            int.TryParse(s.Replace("fsmin", "").Trim(), out int sizeMin);
                            if (sizeMin > 0)
                                __instance.fontSizeMin = sizeMin;
                            break;
                        case string s when s.Contains("fsmax"):
                            int.TryParse(s.Replace("fsmax", "").Trim(), out int sizeMax);
                            if (sizeMax > 0)
                                __instance.fontSizeMax = sizeMax;
                            break;
                        case string s when s.Contains("alignmentcenter"):
                            __instance.alignment = TextAlignmentOptions.Center;
                            break;
                        case string s when s.Contains("alignmenttop"):
                            __instance.alignment = TextAlignmentOptions.Top;
                            break;
                        case string s when s.Contains("alignmenttopright"):
                            __instance.alignment = TextAlignmentOptions.TopRight;
                            break;
                        case string s when s.Contains("characterspacing"):
                            float.TryParse(s.Replace("characterspacing", "").Trim(), out float charSpacing);
                            __instance.characterSpacing = charSpacing;
                            break;
                        case string s when s.Contains("linespacing"):
                            float.TryParse(s.Replace("linespacing", "").Trim(), out float lineSpacingVal);
                            __instance.lineSpacing = lineSpacingVal;
                            break;
                        case "resetrotation":
                            __instance.transform.localRotation = Quaternion.identity;
                            break;
                        case string s when s.Contains("anchoredposition"):
                            float xPos2 = gameObject.GetComponent<RectTransform>().anchoredPosition.x;
                            float yPos2 = gameObject.GetComponent<RectTransform>().anchoredPosition.y;
                            foreach (string element in setting.Replace("anchoredposition", "").Trim().Split(' ', System.StringSplitOptions.None))
                            {
                                float.TryParse(element.Replace("x", "").Replace("y", ""), out float number);
                                if (element.Contains("x"))
                                    xPos2 = Mathf.Approximately(0f, number) ? xPos2 : number;
                                else if (element.Contains("y"))
                                    yPos2 = Mathf.Approximately(0f, number) ? yPos2 : number;
                            }
                            gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos2, yPos2);
                            break;

                    }
                }
            }
        }

    }


    [HarmonyPatch(typeof(InputFieldTempModifier), "Modify")]
    [HarmonyPriority(800)]
    [HarmonyPostfix]
    private static void TMP_InputField_OnEnable_Post(InputFieldTempModifier __instance, ref int maxLength, bool allowNonLatin)
    {

        __instance.allowNonLatin = true;
        // PlayerCreateUI(Clone)/RoleName/Name
    }

    [HarmonyPatch(typeof(LocalizationComponent), "Awake")]
    [HarmonyPriority(800)]
    [HarmonyPostfix]
    private static void LocalizationComponent_Awake_Postfix(LocalizationComponent __instance)
    {

        if (WulinModConfig.Dump == true)
        {
            Il2CppReferenceArray<LocData> locDatas = BaseDataClass.GetGameData<LocDataScriptObject>().LocData;

            JArray jsonArray = new JArray();

            for (int i = 0; i < locDatas.Length; i++)
            {
                LocData locData = locDatas[i];

                JObject jsonObject = new JObject();
                jsonObject["key"] = locData.UName;
                jsonObject["original"] = locData.English;
                jsonObject["translation"] = "";
                // jsonObject["context"] = locData.SChinese;

                jsonArray.Add(jsonObject);
            }
            string json = jsonArray.ToString();
            File.WriteAllText(Path.Combine(BepInExLoader.assemblyFolder, "Dump", "LocData.json"), json);
        }

    }

}
