using BepInEx;
using BepInEx.Logging;
using CSVHelper;
using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace EnglishPatch;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MainPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
  
    private void Awake()
    {
        Logger = base.Logger;

        // Plugin startup logic
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Harmony.CreateAndPatchAll(typeof(MainPlugin));
        //Harmony.CreateAndPatchAll(typeof(TextAssetPatch));
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} should be patched!");
    }
    
    public void OnDestroy()
    {
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is destroyed!");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SweetPotato.DataMgr), "Init")]
    public static bool Init()
    {
        Logger.LogWarning($"Hooked Init!");
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SweetPotato.DataMgr), "LoadDB")]
    public static bool LoadDB(DataMgr __instance, Dictionary<string, CsvLoader.CsvCreateFunc> m_dic_csv)
    {
        Logger.LogWarning($"Hooked LoadDB!");
        var resourcesFolder = Path.Combine(Environment.CurrentDirectory, "resources");
        Logger.LogInfo($"Resources Folder: {resourcesFolder}");

        //Logger.LogWarning($"Current Db File: {AppGame.Instance?.dbVersionFilePath}");

        var dbFile = $"{resourcesFolder}/Db1.txt";
        Logger.LogInfo($"New Db file: {resourcesFolder}");
        if (File.Exists(dbFile))
        {
            AppGame.Instance.dbVersionFilePath = dbFile;
            Logger.LogWarning($"Loading modded file!!");
        }

        // Return true to let the original LoadDB execute
        return true;
    }

    //[HarmonyPostfix, HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.PreLoadAssetBundle))]
    //private static void PreLoadAssetBundle(ResourceManager __instance)
    //{
    //    var bundle = __instance.GetLoadedBundle("Gui/gui");
    //    var exportedStrings = new List<string>();

    //    foreach (var assetName in bundle.GetAllAssetNames())
    //    {
    //        var asset = bundle.LoadAsset(assetName);
    //        if (asset is GameObject gameObject)
    //        {
    //            foreach (var component in gameObject.GetComponentsInChildren<UnityEngine.Component>(true))
    //            {
    //                AddToExportedStrings(exportedStrings, "m_Text", assetName, component.name, component);
    //                AddToExportedStrings(exportedStrings, "m_text", assetName, component.name, component);
    //            }
    //        }
    //    }

    //    File.WriteAllLines(@"C:/debug/1.txt", exportedStrings);
    //    Logger.LogWarning("Exported Prefabs");
    //}

    //private static void AddToExportedStrings(List<string> exportedStrings, string propertyName, string assetName, string componentName, object component)
    //{
    //    Type type = component.GetType();
    //    var textField = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    //    if (textField != null && textField.FieldType == typeof(string))
    //    {
    //        var textValue = textField.GetValue(component) as string;
    //        if (!string.IsNullOrEmpty(textValue))
    //        {
    //           // textField.SetValue(component, "Resource Hijacked!"); // This won't work im loading a new instance of an asset
    //            exportedStrings.Add($"{propertyName} {assetName} = {componentName} =  {textValue}");
    //        }
    //    }
    //}

    // Doesn't like being hooked
    //[HarmonyPostfix, HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset))]
    //public static void LoadAsset(ref UnityEngine.Object __result, string name)
    //{
    //    HijackAsset("m_Text", __result);
    //    HijackAsset("m_text", __result);
    //}

    //private static void HijackAsset(string propertyName, object component)
    //{
    //    Type type = component.GetType();
    //    var textField = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    //    if (textField != null && textField.FieldType == typeof(string))
    //    {
    //        var textValue = textField.GetValue(component) as string;
    //        if (!string.IsNullOrEmpty(textValue))
    //            textField.SetValue(component, "Resource Hijacked!");
    //    }
    //}

    //[HarmonyPostfix, HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetGui))]
    //public static void GetGui(ref UnityEngine.Object __result, string name)
    //{
    //    // Check if the result is a TMP_Text object (or any class that has 'm_text' property)
    //    if (__result is TMPro.TMP_Text tmpText)
    //    {
    //        // Modify the m_text property
    //        tmpText.text = "New text value";  // Set this to whatever you need

    //        // Optionally log the change
    //        Logger.LogWarning($"Modified m_text for TMP_Text with name: {name}, new text: {tmpText.text}");
    //    }
    //    Logger.LogWarning($"Not sure what I am name: {name}");
    //}

    //[HarmonyPrefix, HarmonyPatch(typeof(TMP_Text), "text", MethodType.Setter)]
    //public static bool TextSetter(string value, TMP_Text __instance)
    //{
    //    //This definitely works but it gets EVERYTHING
    //    Logger.LogWarning($"TextSetter2: {value}");
    //    return true;
    //}

    //Seems to be for other stuff like newlinesbefore
    //[HarmonyPatch(typeof(TextAsset), "text", MethodType.Getter)]
    //[HarmonyPrefix]
    //static bool TextGetter(ref string __result, TextAsset __instance)
    //{
    //    Logger.LogWarning($"Hooked you! {__instance.name}");
    //    return true;
    //}
}