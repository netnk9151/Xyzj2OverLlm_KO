using BepInEx;
using BepInEx.Logging;
using CSVHelper;
using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
            Logger.LogWarning($"Loading modded file!");
        }

        // Return true to let the original LoadDB execute
        return true;
    }


    [HarmonyPrefix, HarmonyPatch(typeof(ModSpace.DataMgr), nameof(ModSpace.DataMgr.LoadDB))]
    public static bool LoadDB2(int modId, string dataName)
    {
        Logger.LogWarning($"ModDb2: {modId}");
        return true;
    }
}
