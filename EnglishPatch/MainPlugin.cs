using BepInEx;
using BepInEx.Logging;
using CSVHelper;
using EnglishPatch.Patches;
using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using static TMPro.TMP_Settings;

namespace EnglishPatch;

/// <summary>
/// Swaps the Text db asset in
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MainPlugin : BaseUnityPlugin
{
    public const string ChineseCharPattern = @".*\p{IsCJKUnifiedIdeographs}.*";
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

        // Plugin startup logic
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Harmony.CreateAndPatchAll(typeof(MainPlugin));
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} should be patched!");

        // Disable LeadingCharacters and FollowingCharacters line break rules
        //DisableTMPLineBreakRules();
    }

    public void OnDestroy()
    {
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is destroyed!");
    }

    //private void DisableTMPLineBreakRules()
    //{
    //    TMP_Settings settings = TMP_Settings.instance;
    //    if (settings != null)
    //    {
    //        var lineBreakingRules = new LineBreakingTable()
    //        { 
    //            followingCharacters = [], 
    //            leadingCharacters = [] 
    //        };

    //        SetPrivateField(settings, "m_linebreakingRules", lineBreakingRules);
    //        Logger.LogInfo("Disabled LeadingCharacters and FollowingCharacters line break rules for TMP settings.");
    //    }
    //    else
    //    {
    //        Logger.LogError("TMP_Settings instance is null. Cannot disable line break rules.");
    //    }
    //}

    //private void SetPrivateField(object obj, string fieldName, object value)
    //{
    //    FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
    //    if (field != null)
    //        field.SetValue(obj, value);
    //    else
    //        Logger.LogError($"Field '{fieldName}' not found in {obj.GetType().Name}.");
    //}

    //[HarmonyPrefix, HarmonyPatch(typeof(TMP_Settings), "LoadLinebreakingRules")]
    //public static bool Prefix_TMP_Settings_LoadLinebreakingRules()
    //{
    //    Logger.LogWarning($"Hooked LoadLinebreakingRules!");

    //    // Use reflection to access the private static field s_Instance
    //    var instanceFieldInfo = typeof(TMP_Settings).GetField("s_Instance", BindingFlags.NonPublic | BindingFlags.Static);
    //    if (instanceFieldInfo == null)
    //    {
    //        Logger.LogError("Field 's_Instance' not found.");
    //        return true; // Let the original method execute if the field is not found
    //    }

    //    var instance = (TMP_Settings)instanceFieldInfo.GetValue(null);
    //    if (instance == null)
    //    {
    //        Logger.LogError("Instance of TMP_Settings is null.");
    //        return false; // Haven't loaded yet so we're good
    //    }

    //    // Use reflection to access the private instance field m_linebreakingRules
    //    var fieldInfo = typeof(TMP_Settings).GetField("m_linebreakingRules", BindingFlags.NonPublic | BindingFlags.Instance);
    //    if (fieldInfo == null)
    //    {
    //        Logger.LogError("Field 'm_linebreakingRules' not found.");
    //        return true; // Let the original method execute if the field is not found
    //    }

    //    var rules = (LineBreakingTable)fieldInfo.GetValue(instance);
    //    if (rules == null)
    //    {
    //        Logger.LogInfo("Initializing LineBreakingTable.");
    //        rules = new LineBreakingTable();
    //        fieldInfo.SetValue(null, rules);
    //    }
    //    else
    //    {
    //        Logger.LogFatal("Resetting LineBreakingTable.");
    //        rules.leadingCharacters = [];
    //        rules.followingCharacters = [];
    //    }

    //    Logger.LogInfo("Disabled LeadingCharacters and FollowingCharacters line break rules for TMP settings.");
    //    //Logger.LogError($"Disabled leadingCharacters: {TMP_Settings.leadingCharacters}");
    //    //Logger.LogError($"Disabled followingCharacters: {TMP_Settings.followingCharacters}");

    //    return false; // Skip the original method
    //}

    [HarmonyPrefix, HarmonyPatch(typeof(TMP_Settings), "GetCharacters")]
    public static bool Prefix_TMP_Settings_GetCharacters(ref Dictionary<int, char> __result)
    {
        __result = [];
        return false;
    }

    // Replace assets with translated assets
    [HarmonyPrefix, HarmonyPatch(typeof(DataMgr), "LoadDB")]
    public static bool Prefix_DataMgr_LoadDB(DataMgr __instance, Dictionary<string, CsvLoader.CsvCreateFunc> m_dic_csv)
    {
        Logger.LogWarning($"Hooked LoadDB!");
        var resourcesFolder = Path.Combine(Paths.BepInExRootPath, "resources");
        var dbFile = $"{resourcesFolder}/db1.txt";

        if (File.Exists(dbFile))
        {
            AppGame.Instance.dbVersionFilePath = dbFile;
            Logger.LogInfo($"Loading Translated Assets file: {dbFile}");
        }
        else
            Logger.LogFatal($"Failed to load Translated Assets file: {dbFile}");

        //Return true to let the original LoadDB execute
        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(DataMgr), "LoadDB")]
    public static void Postfix_DataMgr_LoadDB(DataMgr __instance, Dictionary<string, CsvLoader.CsvCreateFunc> m_dic_csv)
    {
        Logger.LogWarning($"Translated Assets Loaded!");
    }
}