using BepInEx;
using BepInEx.Logging;
using CSVHelper;
using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using TriangleNet;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static MouseSimulator;

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
    }

    public void OnDestroy()
    {
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is destroyed!");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MessageBox), nameof(MessageBox.Show))]
    public static bool Prefix_MessageBox_Show(MessageBox __instance, string txt, UnityAction call, bool confirmBtnOnly, string confirmBtxTex, string cancleBtnTxt, UnityAction cancelAtion)
    {
        Logger.LogWarning("Hooked MessageBox!");

        //SweetPotato.InstantiateViewNewNew
        //MessageBox.

        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MessageBox), nameof(MessageBox.Show))]
    public static void Prefix_MessageBox_Show(MessageBox __instance)
    {
        Logger.LogWarning("Hooked Postfix MessageBox!");

        var txtContent = AccessTools.Field(typeof(MessageBox), "txtContent").GetValue(__instance) as TextMeshProUGUI;
        Logger.LogWarning($"Text: {txtContent?.text} Size = {txtContent?.fontSize}");
        //SweetPotato.InstantiateViewNewNew
        //MessageBox.
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

        // Old Code
        //OriginalLoadDbCode(__instance, m_dic_csv);
        //Logger.LogWarning("All good!");

        //Return true to let the original LoadDB execute
        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(DataMgr), "LoadDB")]
    public static void Postfix_DataMgr_LoadDB(DataMgr __instance, Dictionary<string, CsvLoader.CsvCreateFunc> m_dic_csv)
    {
        Logger.LogWarning($"Translated Assets Loaded!");
    }

    //Remove Name Restrictions
    [HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.InstantiateViewNewNew_mobile), "Awake")]
    public static void Postfix_InstantiateViewNewNew_mobile_Awake(InstantiateViewNewNew_mobile __instance)
    {
        Logger.LogWarning("Postfix_InstantiateViewNewNew_mobile_Awake");

        var nameInput = AccessTools.Field(typeof(InstantiateViewNewNew_mobile), "m_nameinput").GetValue(__instance) as TMP_InputField;
        var nextButton = AccessTools.Field(typeof(InstantiateViewNewNew_mobile), "m_NextBuBtn").GetValue(__instance) as Button;

        Logger.LogWarning($"Hooked InstantiateViewNewNew Awake! {nameInput} {nextButton}");

        if (nameInput != null && nextButton != null)
        {
            nameInput.onValueChanged.RemoveAllListeners();
            nameInput.onValueChanged.AddListener((string newStr) =>
            {
                // Always allow the name, removing keyword restrictions
                nextButton.interactable = Tools.IsStrAvailable(newStr);
            });

            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener((string newStr) =>
            {
                // Always allow the name, removing keyword restrictions
                nextButton.interactable = Tools.IsStrAvailable(newStr);
            });
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Form), "Awake")]
    public static void Postfix_Form_Awake(Form __instance)
    {
        Logger.LogInfo($"Identified Form: {__instance.GetType()}");
    }

    private static void OriginalLoadDbCode(DataMgr __instance, Dictionary<string, CsvLoader.CsvCreateFunc> m_dic_csv)
    {
        var bs = DataMgr.ReadFileToBytes(AppGame.Instance.dbVersionFilePath);
        var stopwatch = Stopwatch.StartNew();
        if (AppGame.Instance.m_LoadDBUseTask)
        {
            Dictionary<string, List<string[]>> dictionary = new Dictionary<string, List<string[]>>();
            string[] array = Encoding.UTF8.GetString(bs).Split('\n');
            for (int i = 0; i < array.Length; i++)
            {
                string text = array[i];
                string[] array2 = text.TrimEnd('\r').Split('|');
                if (array2.Length == 2)
                {
                    string key = array2[0];
                    int num = int.Parse(array2[1]);
                    dictionary[key] = new List<string[]>();
                    for (int j = 0; j < num; j++)
                    {
                        string text2 = array[i + 1 + j];
                        dictionary[key].Add(text2.Split('#'));
                    }

                    i += num;
                }
                else
                {
                    UnityEngine.Debug.LogErrorFormat(i + " " + text);
                }
            }

            List<Task> list = new List<Task>();
            foreach (KeyValuePair<string, List<string[]>> item in dictionary)
            {
                string tableName = item.Key;
                Logger.LogWarning($"TableName: {tableName}");
                List<string[]> lists = item.Value;
                list.Add(Task.Run(delegate
                {
                    if (m_dic_csv.TryGetValue(tableName, out var value2))
                    {
                        foreach (string[] item2 in lists)
                        {
                            try
                            {
                                value2(item2);
                            }
                            catch (Exception message)
                            {
                                UnityEngine.Debug.LogError(message);
                            }
                        }
                    }
                }));
            }

            Task.WaitAll(list.ToArray());
            UnityEngine.Debug.LogFormat($"loaddb sb1 process cost {stopwatch.ElapsedMilliseconds}ms totalDataCount: {array.Length}");
        }
        else
        {
            var stopwatch2 = Stopwatch.StartNew();
            string str;
            while (__instance.ReadCsvLine(ref bs, out str))
            {
                string[] array3 = str.Split('|');
                if (array3.Length != 2)
                {
                    break;
                }

                long usedsizeMemory = TestManagedHeap.GetUsedsizeMemory();
                int result = 0;
                int.TryParse(array3[1], out result);
                string text3 = array3[0];
                if (!m_dic_csv.TryGetValue(text3, out var value) || result == 0)
                {
                    for (int k = 0; k < result; k++)
                    {
                        __instance.ReadCsvLine(ref bs, out var _);
                    }

                    continue;
                }

                try
                {
                    Logger.LogWarning($"I think tableName: {text3}");
                    if (text3.Equals(ItemPrototype.GetTableName()) && !RoleBagDataModel.m_bSetAllItemBaseData)
                    {
                        RoleBagDataModel.m_AllItemBase = new List<Item>();
                    }

                    for (int l = 0; l < result; l++)
                    {
                        if (__instance.ReadCsvLine(ref bs, out var str3))
                        {
                            string[] array4 = str3.Split('#');
                            value(array4);
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("DB sometime somethin");
                        }
                    }

                    if (text3.Equals(ItemPrototype.GetTableName()) && !RoleBagDataModel.m_bSetAllItemBaseData)
                    {
                        RoleBagDataModel.m_bSetAllItemBaseData = true;
                    }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                    UnityEngine.Debug.LogError("table name:" + array3[0] + " DB Loaded");
                }

                if (text3.Equals(Stringlang.GetTableName()))
                {
                    Stringlang.Clear();
                }

                if (text3.Equals(NpcAttriDynamic.GetTableName()))
                {
                    NpcAttriDynamic.Clear();
                }

                long num2 = TestManagedHeap.GetUsedsizeMemory() - usedsizeMemory;
                //if (num2 > 1048576)
                //{
                UnityEngine.Debug.Log("load table:" + array3[0] + "   Memory:" + (float)num2 * 1f / 1024f / 1024f + "M");
                //}
            }

            GC.Collect(0, GCCollectionMode.Optimized);
            UnityEngine.Debug.LogFormat("loaddb sb1 cost {0}ms", stopwatch2.ElapsedMilliseconds);
            stopwatch2.Restart();
        }
    }
}