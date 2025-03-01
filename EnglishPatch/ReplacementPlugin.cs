using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace EnglishPatch;

[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.TextReplacer", "MonoBehavior Text Replacer", MyPluginInfo.PLUGIN_VERSION)]
public class TextReplacerPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogWarning("Text Replacer plugin is starting...");
        Harmony.CreateAndPatchAll(typeof(PrefabTextPatch));
        Logger.LogWarning("Text Replacer plugin patching complete!");
    }

    [HarmonyPatch(typeof(UnityEngine.Object))]
    public static class PrefabTextPatch
    {       
        // Patch Instantiate(GameObject)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(GameObject) })]
        [HarmonyPostfix]
        static void Postfix1(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

        // Patch Instantiate(GameObject, Transform)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(GameObject), typeof(Transform) })]
        [HarmonyPostfix]
        static void Postfix2(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

        // Patch Instantiate(GameObject, Transform, bool)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(GameObject), typeof(Transform), typeof(bool) })]
        [HarmonyPostfix]
        static void Postfix3(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

        private static void UpdateText(UnityEngine.Object __result)
        {
            if (__result is GameObject gameObject)
            {
                if (__result.name.Contains("LoginView"))
                    return;

                foreach (var textComponent in gameObject.GetComponentsInChildren<TextMeshProUGUI>(true))
                {                
                    var text = textComponent.text;
                    if (string.IsNullOrEmpty(text))
                        return;

                    if (Regex.IsMatch(text, MainPlugin.ChineseCharPattern))
                    {
                        //Logger.LogWarning($"Replaced {__result.name}");
                        //if (!__result.name.Contains("SettingView"))
                        //    return;

                        //Logger.LogWarning($"Replaced {text}");
                        textComponent.text = "New Updated Text"; // Modify as needed
                    }
                }
            }
        }

        //[HarmonyPrefix, HarmonyPatch(typeof(TMP_Text), "text", MethodType.Setter)]
        //public static bool TextSetter(string value, TMP_Text __instance)
        //{
        //    //This definitely works but it gets EVERYTHING
        //    Logger.LogWarning($"TextSetter2: {value}");
        //    return true;
        //}
    }

}