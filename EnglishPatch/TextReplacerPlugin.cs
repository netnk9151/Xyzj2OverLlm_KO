using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace EnglishPatch;

[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.TextReplacer", "TextReplacer", MyPluginInfo.PLUGIN_VERSION)]
public class TextReplacerPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Dictionary<string, string> Replacements = [];

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogWarning("Text Replacer plugin is starting...");
        Harmony.CreateAndPatchAll(typeof(PrefabTextPatch));
        Logger.LogWarning("Text Replacer plugin patching complete!");


        Logger.LogWarning("Loading Prefab Replacements...");
        var resourcesFolder = Path.Combine(Environment.CurrentDirectory, "BepInEx/resources");
        var resourcesFolder2 = Path.Combine(Environment.CurrentDirectory, "resources"); //Autotranslator messes with this
        var dbFile = $"{resourcesFolder}/dumpedPrefabText.txt";
        var dbFile2 = $"{resourcesFolder2}/dumpedPrefabText.txt";

        string[] lines = [];

        if (File.Exists(dbFile))
            lines = File.ReadAllLines(dbFile);

        if (File.Exists(dbFile2))
            lines = File.ReadAllLines(dbFile2);

        for (int i = 0; i < lines.Length; i = i + 2)
        {
            var raw = lines[i].Replace("- raw: ", "").Replace("\\n", "\n") ; //Do not trim some of these have spacing
            var result = lines[i + 1].Replace("result: ", "").Replace("\\n", "\n");

            Replacements.Add(raw, result);
        }
    }

    [HarmonyPatch(typeof(UnityEngine.Object))]
    public static class PrefabTextPatch
    {
        //Patch Instantiate(Object)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(UnityEngine.Object) })]
        [HarmonyPostfix]
        static void ObjectPostfix1(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

        // Patch Instantiate(GameObject, Transform)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(UnityEngine.Object), typeof(Transform) })]
        [HarmonyPostfix]
        static void ObjectPostfix2(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

        // Patch Instantiate(GameObject, Transform, bool)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(UnityEngine.Object), typeof(Transform), typeof(bool) })]
        [HarmonyPostfix]
        static void ObjectPostfix3(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

        // Patch Instantiate(GameObject, Transform, bool)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(UnityEngine.Object), typeof(Vector3), typeof(Quaternion) })]
        [HarmonyPostfix]
        static void ObjectPostfix4(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

        // Patch Instantiate(GameObject, Transform, bool)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(UnityEngine.Object), typeof(Vector3), typeof(Quaternion), typeof(Transform) })]
        [HarmonyPostfix]
        static void ObjectPostfix5(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

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

        // Patch Instantiate(GameObject, Transform, bool)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion) })]
        [HarmonyPostfix]
        static void Postfix4(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

        // Patch Instantiate(GameObject, Transform, bool)
        [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new Type[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Transform) })]
        [HarmonyPostfix]
        static void Postfix5(ref UnityEngine.Object __result)
        {
            UpdateText(__result);
        }

        private static void UpdateText(UnityEngine.Object __result)
        {
            if (__result is GameObject gameObject)
            {
                if (__result.name.Contains("LoginView"))
                    return;
                // Need to figure out how to ignore buttons here - causes grief
                //[Info   : Unity Log] Selection: new (btn (1) (UnityEngine.GameObject)) old (null)


                foreach (var component in gameObject.GetComponentsInChildren<UnityEngine.Component>(true))
                {
                    var response = string.Empty;

                    var textField = component.GetType().GetField("m_text", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    // Try mistyped property
                    if (textField == null)
                        textField = component.GetType().GetField("m_Text", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (textField != null && textField.FieldType == typeof(string))
                    {
                        var textValue = textField.GetValue(component) as string;

                        if (string.IsNullOrEmpty(textValue))
                            continue; 

                        if (Replacements.ContainsKey(textValue))
                            textField.SetValue(component, Replacements[textValue]);
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