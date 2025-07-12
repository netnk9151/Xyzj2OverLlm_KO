using BepInEx;
using BepInEx.Logging;
using EnglishPatch.DynamicStrings;
using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace EnglishPatch.PrefabText;

/// <summary>
/// Used to get hardcoded strings out of prefabs so we can translate them
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.Dumper", "TextDumper", MyPluginInfo.PLUGIN_VERSION)]
public class TextDumperPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    //Do not want dumper outside of development 
    public static bool Enabled = StringDumperPlugin.Enabled;

    private void Awake()
    {
        Logger = base.Logger;

        if (!Enabled)
            return;

        Logger.LogWarning("Text Replacer plugin is starting...");
        Harmony.CreateAndPatchAll(typeof(TextDumperPlugin));
        Logger.LogWarning("Text Dumper plugin patching complete!");
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.PreLoadAssetBundle))]
    private static void PreLoadAssetBundle(ResourceManager __instance)
    {
        var exportedStrings = new List<string>();
        string[] bundles = [
            "Gui/gui", 
            //"Prefab/prefab", 
            //"Prefab/prefabmix", 
            //"MyAssets/myasset"
        ];

        foreach (var bundleName in bundles)
        {
            var bundle = __instance.GetLoadedBundle(bundleName);

            foreach (var assetName in bundle.GetAllAssetNames())
            {
                var asset = bundle.LoadAsset(assetName);

                if (asset is GameObject gameObject)
                {
                    foreach (var component in gameObject.GetComponentsInChildren<Component>(true))
                    {
                        try
                        {
                            var text = GetValidTextProperty(component);
                            if (string.IsNullOrEmpty(text))
                                continue;

                            // Clean up for dump
                            text = text.Replace("\n", "\\n");

                            if (!exportedStrings.Contains(text))
                                exportedStrings.Add(text);
                        }
                        catch (Exception ex)
                        {
                            // Ignore errors
                            Logger.LogError($"Error getting text from component in prefab {assetName}: {ex.Message}");
                        }
                    }
                }
            }
        }

        File.WriteAllLines(@"G:/Xyzj2OverLlm/Files/Raw/ExportedText/dumpedPrefabText.txt", exportedStrings);
        Logger.LogWarning("Exported Prefabs");
    }

    private static string GetValidTextProperty(object component)
    {
        var response = string.Empty;

        if (component is null)
            return response;

        var type = component.GetType();

        if (type is null)
            return response;

        var textField =
            type.GetField("m_text", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? type.GetField("m_Text", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (textField != null && textField.FieldType == typeof(string))
        {
            var textValue = textField.GetValue(component) as string;
            if (!string.IsNullOrEmpty(textValue) && Regex.IsMatch(textValue, MainPlugin.ChineseCharPattern))
                response = textValue;
        }

        return response;
    }
}
