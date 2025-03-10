using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SweetPotato;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EnglishPatch;

/// <summary>
/// Put dicey stuff in here that might crash the plugin - so it doesnt crash the existing plugins
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.Debug", "DebugGame", MyPluginInfo.PLUGIN_VERSION)]
internal class DebugPlugin: BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(typeof(DebugPlugin));
        Logger.LogWarning($"Debug Game Plugin should be patched!");
    }

    private void Test()
    {
//        [Error: Unity Log] IndexOutOfRangeException: Index was outside the bounds of the array.
//Stack trace:
//(wrapper dynamic - method) MainView.DMD<MainView::RefreshDateTimeText>(MainView)
//MainView.Update()(at<afc667d1e3284cde8a140a8b5bc9a47f>:0)

        //MainView
        //RoleBagItemListHolder
    }

    // Opening Screen
    //[HarmonyPrefix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonClick")]
    //public static bool Prefix_OnButtonClick(SweetPotato.LoginViewNew __instance, MethodBase __originalMethod, int index, RectTransform rect)
    //{
    //    //InstructionLogger.LogInstructions(__originalMethod);

    //    var text = rect.FindChildCustom<TextMeshProUGUI>("btnname").text.Trim();

    //    Logger.LogWarning($"Hooked PREFIX OnButtonClick! [{text}]");

    //    switch (rect.FindChildCustom<TextMeshProUGUI>("btnname").text.Trim())
    //    {
    //        case "新的江湖":
    //            Logger.LogWarning($"Old Text");
    //            break;
    //        case "A new Jianghu":
    //            Logger.LogWarning($"New Text");
    //            break;
    //    }

    //    return true;
    //}

    //// Opening Screen
    //[HarmonyPrefix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonClick")]
    //public static void Postfix_OnButtonClick()
    //{
    //    Logger.LogWarning($"Hooked POSTFIX OnButtonClick!");
    //}

    ////[HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonClick")]
    ////public static void Postfix_OnButtonClick(IEnumerable<CodeInstruction> __instructions)
    ////{
    ////    InstructionLogger.LogInstructions(__instructions);
    ////}

    //[HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonNewGame")]
    //public static void Postfix_LoginViewNew_OnButtonNewGame()
    //{
    //    Logger.LogWarning("Hooked OnButtonNewGame!");
    //}
}

public class InstructionLogger
{
    public static void LogInstructions(MethodBase method)
    {
        DebugPlugin.Logger.LogFatal($"Logging instructions for method: {method.Name}");

        //var instructions = PatchProcessor.GetOriginalInstructions(method);
        var instructions = PatchProcessor.GetCurrentInstructions(method);
        foreach (var instruction in instructions)
        {
            DebugPlugin.Logger.LogFatal($"Instruction: {instruction.opcode} {instruction.operand}");
        }
    }

    public static void LogInstructions(IEnumerable<CodeInstruction> instructions)
    {
        DebugPlugin.Logger.LogFatal("Logging modified instructions:");
        foreach (var instruction in instructions)
        {
            DebugPlugin.Logger.LogFatal($"Instruction: {instruction.opcode} {instruction.operand}");
        }
    }
}
