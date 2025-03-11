using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace EnglishPatch.Patches;

[HarmonyPatch(typeof(MainView))]
[HarmonyPatch("RefreshDateTimeText")]
public static class MinimapPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var from = AccessTools.Method(typeof(SweetPotato.Tools), nameof(SweetPotato.Tools.GetGameTimeDate));
        var to = AccessTools.Method(typeof(MinimapPatch), "MyGetGameTimeDate");
        return Transpilers.MethodReplacer(instructions, from, to);
    }

    public static string MyGetGameTimeDate(double mainQuestTimeSecond)
    {        
        return SweetPotato.Tools.GetGameTimeDate(mainQuestTimeSecond).Replace("Year", "年");
    }
}