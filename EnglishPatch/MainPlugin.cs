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
using System.Text;
using System.Threading.Tasks;

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
        Harmony.CreateAndPatchAll(typeof(NameRestrictionPatch));
        Harmony.CreateAndPatchAll(typeof(MinimapPatch));
        Harmony.CreateAndPatchAll(typeof(ItemsPatch));
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} should be patched!");
    }

    public void OnDestroy()
    {
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is destroyed!");
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

    [HarmonyPrefix, HarmonyPatch(typeof(UnityHelper), "GetNunWordFromNum")]
    public static bool Prefix_UnityHelper_GetNunWordFromNum(ref string __result, int num)
    {
        __result = $"{num}";
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SweetPotato.Tools), "NumberToChinese")]
    public static bool Prefix_Tools_NumberToChinese(ref string __result, int number, bool first)
    {
        __result = $"{number}";
        return false;
    }
}