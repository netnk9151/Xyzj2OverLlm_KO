using BepInEx;
using BepInEx.Logging;
using CSVHelper;
using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.IO;

namespace EnglishPatch;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MainPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    readonly static string ModRootPath = Path.Combine(Environment.CurrentDirectory, "BepInEx/resources");

    public void Start()
    {
        Harmony.CreateAndPatchAll(typeof(MainPlugin));
    }

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DataMgr), "LoadDB")]
    public static bool LoadDB(DataMgr __instance, Dictionary<string, CsvLoader.CsvCreateFunc> m_dic_csv)
    {
        Logger.LogWarning($"Current Path: {AppGame.Instance.dbVersionFilePath}");
        AppGame.Instance.dbVersionFilePath = $"{ModRootPath}/Db1.txt";
        Logger.LogWarning($"Loading modded file!");

        // Return true to let the original LoadDB execute
        return true;
    }
}
