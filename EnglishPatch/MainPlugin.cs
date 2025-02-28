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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TriangleNet;
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
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} should be patched!");
    }

    public void OnDestroy()
    {
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is destroyed!");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DataMgr), "Init")]
    public static bool Init()
    {
        Logger.LogWarning($"Hooked Init!");
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DataMgr), "LoadDB")]
    public static bool Prefix_LoadDB(DataMgr __instance, Dictionary<string, CsvLoader.CsvCreateFunc> m_dic_csv)
    {
        Logger.LogWarning($"Hooked LoadDB!");
        var resourcesFolder = Path.Combine(Environment.CurrentDirectory, "BepInEx/resources");
        var resourcesFolder2 = Path.Combine(Environment.CurrentDirectory, "resources"); //Autotranslator messes with this
        var dbFile = $"{resourcesFolder}/db1.txt";
        var dbFile2 = $"{resourcesFolder2}/db1.txt";

        if (File.Exists(dbFile))
        {
            AppGame.Instance.dbVersionFilePath = dbFile;
            Logger.LogInfo($"Loading Translated Assets file: {dbFile}");
        }
        if (File.Exists(dbFile2))
        {
            AppGame.Instance.dbVersionFilePath = dbFile2;
            Logger.LogInfo($"Loading Translated Assets file: {dbFile2}");
        }

        // Old Code
        //OldLoadDbCode(__instance, m_dic_csv);
        //Logger.LogWarning("All good!");

        //Return true to let the original LoadDB execute
        return true;
    }

    private static void OldLoadDbCode(DataMgr __instance, Dictionary<string, CsvLoader.CsvCreateFunc> m_dic_csv)
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

    [HarmonyPostfix, HarmonyPatch(typeof(DataMgr), "LoadDB")]
    public static void Post_LoadDB(DataMgr __instance, Dictionary<string, CsvLoader.CsvCreateFunc> m_dic_csv)
    {
        Logger.LogWarning($"Translated Assets Loaded!");
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