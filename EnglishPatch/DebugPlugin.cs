using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

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
        
    }   

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
